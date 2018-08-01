using cn.jpush.api;
using cn.jpush.api.common;
using cn.jpush.api.push;
using cn.jpush.api.push.mode;

namespace WorkFlowLib.Logic
{
    public class JPush
    {
        private JPushClient JPushClient { get; }

        private const string Key = "40e7762569be2f6cec21b500";
        private const string Secret = "ea52eda019e749075860e033";

        public JPush()
        {
            JPushClient = new JPushClient(Key, Secret);
        }

        public bool PushToAndroidByAlias(string content, params string[] alias)
        {
            PushPayload pushPayload = new PushPayload
            {
                platform = Platform.android(),
                audience = Audience.s_alias(alias),
                notification = new Notification().setAlert(content)
            };
            try
            {
                MessageResult result = JPushClient.SendPush(pushPayload);
                return result.isResultOK();
            }
            catch (APIRequestException e)
            {
                
            }
            return false;
        }
    }
}
