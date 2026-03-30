namespace TravelApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("SearchPage", typeof(SearchPage));
            Routing.RegisterRoute("TourDetailPage", typeof(TourDetailPage));
            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("SignUpPage", typeof(SignUpPage));
            Routing.RegisterRoute("ProfilePage", typeof(ProfilePage));
            Routing.RegisterRoute("EditProfilePage", typeof(EditProfilePage));
            Routing.RegisterRoute("DebugRuntimeConsolePage", typeof(DebugRuntimeConsolePage));
        }
    }
}
