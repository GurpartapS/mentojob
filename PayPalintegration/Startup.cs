using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PayPalintegration.Startup))]
namespace PayPalintegration
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
