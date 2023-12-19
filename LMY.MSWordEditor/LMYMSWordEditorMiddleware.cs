using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text;


namespace LMY.MSWordEditor
{
    public class LMYMSWordEditorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly LMYMSWordEditorOptions _options;
        public LMYMSWordEditorMiddleware(RequestDelegate next, LMYMSWordEditorOptions options = null)
        {
            _next = next;
            _options = options ?? new LMYMSWordEditorOptions();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                if (httpContext.Request.Path.ToString().ToLower().Contains("lmy.mswordeditor") ||
                    httpContext.Request.Headers.Any(x => x.Value[0].ToLower().Contains("microsoft office")))
                {
                    ValidateRequest(httpContext);

                    if (httpContext.Request.Method == "OPTIONS")
                    {
                        httpContext.Response.StatusCode = 200;
                        httpContext.Response.Headers.Add("Allow", "OPTIONS, TRACE, GET, HEAD, POST, COPY, PROPFIND, DELETE, MOVE, PROPPATCH, MKCOL, LOCK, UNLOCK");
                        httpContext.Response.Headers.Add("Server", "Microsoft-IIS/10.0");
                        httpContext.Response.Headers.Add("Public", "OPTIONS, TRACE, GET, HEAD, POST, PROPFIND, PROPPATCH, MKCOL, PUT, DELETE, COPY, MOVE, LOCK, UNLOCK");
                        httpContext.Response.Headers.Add("DAV", "1,2,3");
                        httpContext.Response.Headers.Add("MS-Author-Via", "DAV");
                        httpContext.Response.Headers.Add("Persistent-Auth", "true");
                        httpContext.Response.Headers.Add("X-Powered-By", "ASP.NET");
                        httpContext.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                        httpContext.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, LOCK, HEAD");
                        httpContext.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
                        httpContext.Response.Headers.Add("Date", DateTime.UtcNow.ToString("R"));
                        httpContext.Response.Headers.Add("Content-Length", "100");

                        // Additional headers or logic can be added as needed

                        await httpContext.Response.WriteAsync(string.Empty);
                    }
                    else if (httpContext.Request.Method == "HEAD")
                    {
                        string filePath = GetFilePath(httpContext.Request.Path);
                        long contentLength = new FileInfo(filePath).Length;

                        httpContext.Response.Headers.Add("DAV", "1, 2");
                        httpContext.Response.Headers.Add("Allow", "OPTIONS, PROPFIND, GET, HEAD, PUT, DELETE");
                        httpContext.Response.Headers.Add("Content-Length", contentLength.ToString());
                        httpContext.Response.Headers.Add("Content-Type", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");

                        // Additional headers and logic can be added as needed

                        httpContext.Response.StatusCode = 200;
                    }
                    else if (httpContext.Request.Method == "PROPFIND")
                    {
                        string responseXml = GeneratePropfindResponse(httpContext);
                        httpContext.Response.ContentType = "text/xml";
                        await httpContext.Response.WriteAsync(responseXml, Encoding.UTF8);
                    }
                    else if (httpContext.Request.Method == "GET")
                    {
                        if (!await CheckAuthentication(httpContext))
                        {
                            return;
                        }
                        string filePath = GetFilePath(httpContext.Request.Path);
                        if (File.Exists(filePath))
                        {
                            byte[] fileContents = File.ReadAllBytes(filePath);
                            await httpContext.Response.Body.WriteAsync(fileContents, 0, fileContents.Length);
                        }
                        else
                        {
                            httpContext.Response.StatusCode = 404; // Not Found
                        }
                    }
                    else if (httpContext.Request.Method == "PUT")
                    {
                        if (!await CheckAuthentication(httpContext))
                        {
                            return;
                        }
                        string filePath = GetFilePath(httpContext.Request.Path);

                        // Check if the file is currently locked
                        if (IsFileLocked(filePath))
                        {
                            httpContext.Response.StatusCode = 423; // Locked
                            return;
                        }

                        // Lock the file to prevent concurrent edits
                        LockFile(filePath);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await httpContext.Request.Body.CopyToAsync(fileStream);
                        }

                        // Unlock the file after the edit is completed
                        UnlockFile(filePath);

                        httpContext.Response.StatusCode = 204; // No Content
                    }
                    else if (httpContext.Request.Method == "LOCK")
                    {
                        httpContext.Response.StatusCode = 200;
                        httpContext.Response.Headers.Add("Lock-Token", $"<urn:uuid:{Guid.NewGuid()}>");
                        httpContext.Response.Headers.Add("Content-Type", "text/xml; charset=utf-8");

                        // Sample response body with lock information
                        string responseBody = GenerateLockResponse(httpContext);

                        httpContext.Response.Headers.Add("Content-Length", responseBody.Length.ToString());

                        await httpContext.Response.WriteAsync(responseBody, Encoding.UTF8);
                    }
                    else if (httpContext.Request.Method == "UNLOCK")
                    {
                        // Sample response headers to simulate a successful UNLOCK response
                        httpContext.Response.StatusCode = 204; // No Content
                        httpContext.Response.Headers.Add("Content-Type", "text/xml; charset=utf-8");
                    }
                    else if (httpContext.Request.Method == "POST")
                    {
                        // Sample response headers for a successful POST request
                        httpContext.Response.StatusCode = 200;
                        httpContext.Response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

                        // Sample response body with a message
                        string responseBody = "POST request processed successfully";

                        httpContext.Response.Headers.Add("Content-Length", responseBody.Length.ToString());

                        await httpContext.Response.WriteAsync(responseBody, Encoding.UTF8);
                    }
                }
                else
                {
                    // rewind the stream for the next middleware
                    httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
                    await _next.Invoke(httpContext);
                }
            }
            catch (Exception ex)
            {
                if (_options.OnError != null)
                {
                    _options.OnError.Invoke("LMYMSWordEditor Error:" + ex.Message, httpContext);
                }
            }
        }
        private async Task<bool> CheckAuthentication(HttpContext httpContext)
        {
            if (_options.OnAuthentication != null)
            {
                var isAuthenticated = _options.OnAuthentication(GetTokenFromUrl(httpContext.Request.Path), httpContext);
                if (!isAuthenticated)
                {
                    httpContext.Response.StatusCode = 401;
                    await httpContext.Response.WriteAsync(string.Empty);
                    return false;
                }
            }

            return true;
        }

        private string GeneratePropfindResponse(HttpContext httpContext)
        {
            string filePath = GetFilePath(httpContext.Request.Path);
            long contentLength = new FileInfo(filePath).Length;

            // Generate a basic PROPFIND response XML
            return @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                    <D:multistatus xmlns:D=""DAV:"">
                        <D:response>
                            <D:href>" + GetCurrentURL(httpContext) + @"</D:href>
                            <D:propstat>
                                <D:prop>
                                    <D:getcontentlength>" + contentLength + @"</D:getcontentlength>
                                    <!-- Add other properties as needed -->
                                </D:prop>
                                <D:status>HTTP/1.1 200 OK</D:status>
                            </D:propstat>
                        </D:response>
                    </D:multistatus>";
        }

        private string GenerateLockResponse(HttpContext httpContext)
        {
            return @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                    <D:prop xmlns:D=""DAV:"">
                        <D:lockdiscovery>
                            <D:activelock>
                                <D:locktype><D:write/></D:locktype>
                                <D:lockscope><D:exclusive/></D:lockscope>
                                <D:depth>0</D:depth>
                                <D:owner><D:href>" + GetCurrentURL(httpContext) + @"</D:href></D:owner>
                                <D:timeout>Infinite</D:timeout>
                                <D:locktoken>
                                    <D:href>urn:uuid:" + Guid.NewGuid().ToString() + @"</D:href>
                                </D:locktoken>
                            </D:activelock>
                        </D:lockdiscovery>
                    </D:prop>";
        }

        private bool IsFileLocked(string filePath)
        {
            // Check if the file is currently locked
            // You may implement a more robust file locking mechanism based on your needs
            return File.Exists(filePath + ".lock");
        }

        private void LockFile(string filePath)
        {
            // Lock the file to prevent concurrent edits
            File.Create(filePath + ".lock").Dispose();
        }

        private void UnlockFile(string filePath)
        {
            // Unlock the file after the edit is completed
            File.Delete(filePath + ".lock");
        }

        private string GetCurrentURL(HttpContext httpContext)
        {
            return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.QueryString}";
        }

        private string GetFilePath(PathString path)
        {
            string filePath = Path.Combine(_options.PhysicalFolderPath, ClearAndRemoveTokenUrl(path.ToString()));

            if (!File.Exists(filePath))
            {
                throw new Exception("File Not Found, Path " + filePath.ToString());
            }

            return filePath;
        }

        private string ClearAndRemoveTokenUrl(string input)
        {
            input = input.ToLower().Replace("lmy.mswordeditor", "");
            // Find the index of the token= in the string
            int startIndex = input.IndexOf("token=");

            if (startIndex != -1)
            {
                // Find the end of the token value
                int endIndex = input.IndexOf("/", startIndex + "token=".Length);

                if (endIndex != -1)
                {
                    // Extract the substring before the token and after the token
                    string beforeToken = input.Substring(0, startIndex);
                    string afterToken = input.Substring(endIndex);

                    // Combine the two substrings
                    input = beforeToken + afterToken;
                }
            }

            // If the token is not found, return the original string
            return input.ToLower().TrimStart('/');
        }

        private string GetTokenFromUrl(string input)
        {
            // Find the index of the token= in the string
            int startIndex = input.IndexOf("token=");

            if (startIndex != -1)
            {
                // Find the end of the token value
                int endIndex = input.IndexOf("/", startIndex + "token=".Length);

                if (endIndex != -1)
                {
                    // Extract the token value
                    string tokenValue = input.Substring(startIndex + "token=".Length, endIndex - (startIndex + "token=".Length));

                    return tokenValue;
                }
            }

            // If the token is not found, return null or an empty string, depending on your requirements
            return null;
        }

        private void ValidateRequest(HttpContext httpContext)
        {
            if (httpContext.Request.QueryString.HasValue)
            {
                // throw new Exception("Query strings are not allowed to be part of url");
            }
        }
    }
}
