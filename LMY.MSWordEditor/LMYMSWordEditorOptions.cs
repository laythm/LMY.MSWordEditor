using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace LMY.MSWordEditor
{
    public class LMYMSWordEditorOptions
    {
        public string PhysicalFolderPath { get; set; } = "D:\\PhysicalFolderPath";
        public Func<string, HttpContext, bool> OnAuthentication { get; set; }
        public Action<string, HttpContext> OnError { get; set; } 
    }
}