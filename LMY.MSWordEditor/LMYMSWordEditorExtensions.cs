using Microsoft.AspNetCore.Builder;

namespace LMY.MSWordEditor
{
    public static class LMYMSWordEditorExtensions
    {
        public static IApplicationBuilder UseLMYMSWordEditor(
          this IApplicationBuilder app,
          Action<LMYMSWordEditorOptions> setupAction = null)
        {
            var options = new LMYMSWordEditorOptions();

            setupAction?.Invoke(options);

            return app.UseMiddleware<LMYMSWordEditorMiddleware>(options);
        }
    }
}