namespace WorkFlowLib.Results
{
    public class UserProfileResult
    {
        public UserProfile data { get; set; }
    }

    public class UserProfile
    {
        public string Country { get; set; }
        public string Language { get; set; }
        public string[] Authority { get; set; }
        public string UserName { get; set; }
        public string UserType { get; set; }
    }

    public class UserLoginProfile : UserProfile
    {
        public string error { get; set; }
    }
}
