using System.Reflection;
using System.Resources;

namespace VyacheslavsMegaWebApp.Service
{
    // TODO: Make separate file repositories for images and resx
    public class AppResourcesManager
    {
        public AppResourcesManager()
        {
            IdentityErrors = new ResourceManager("AppResources.Resx.IdentityErrors", Assembly.GetExecutingAssembly());
            SharedResources = new ResourceManager("AppResources.Resx.IdentityErrors", Assembly.GetExecutingAssembly());
        }

        #region Resx resousces

        public ResourceManager IdentityErrors { get; private set; }

        public ResourceManager SharedResources { get; private set; }

        #endregion

        #region Images

        public const string EmailLogo = "logoEmail.png";

        #endregion

        public string AppResourcesFolder => Path.Combine(AppContext.BaseDirectory, "AppResources");

        public string GetImage(string imageName)
        {
            return Path.Combine(AppResourcesFolder, "Images", imageName);
        }

       
    }
}
