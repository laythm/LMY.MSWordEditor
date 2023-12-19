# LMY.MSWordEditor
Microsoft office word remote editor,
edit word document by an enduser through APIs, HTTP, with no need for any client application to be installed on enduser machine, it can be used from mobile also 


add the below code into Configure
```
app.UseLMYMSWordEditor(o =>
      {
          o.PhysicalFolderPath = @"D:\MyFolderRoot";
          o.OnAuthentication = (string token, HttpContext httpContext) =>
          {
              //do the validation here
              //return false;
              return true;
          };
          o.OnError = (string error, HttpContext httpContext) =>
          {
             //handle errors here
          };
      });
```

then just hit the below url in the browser (token )
```
      ms-word:ofv|u|http://localhost:6000/LMY.MSWordEditor/token=userToken/document.docx
```
Or without token as below
```
      ms-word:ofv|u|http://localhost:6000/LMY.MSWordEditor/document.docx
```
file path can be as below
```
      ms-word:ofv|u|http://localhost:6000/LMY.MSWordEditor/subfolder1/subfolder2/document.docx
```
