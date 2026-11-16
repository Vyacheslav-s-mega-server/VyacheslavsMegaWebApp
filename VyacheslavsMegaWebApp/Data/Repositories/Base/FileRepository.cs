namespace VyacheslavsMegaWebApp.Data.Repositories.Base
{
    public abstract class FileRepository
    {
        public FileRepository(IWebHostEnvironment env)
        {
            ContentRootPath = env.ContentRootPath;
            WebRootPath = env.WebRootPath;
        }

        protected string ContentRootPath { get; }

        protected string WebRootPath { get; }

        protected string AppResourcesFolder => Path.Combine(ContentRootPath, "AppResources");
    }
}
