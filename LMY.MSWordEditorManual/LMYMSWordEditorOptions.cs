using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace LMY.MSWordEditor
{
    public class LMYMSWordEditorOptions
    {
        public Func<string, HttpContext, bool> OnAuthentication { get; set; }
        public Func<string, string, HttpContext, byte[]> OnGetFile { get; set; }
        public Func<string, string, byte[], HttpContext, bool> OnSaveFile { get; set; }
        public Action<string, HttpContext> OnError { get; set; }
    }
}