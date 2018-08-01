using Dreamlab.Core;
using WorkFlowLib;
using WorkFlowLib.Data;

namespace WorkFlow.Logic
{
    public class WfBadgeQtyReader
    {
        public int? GetQty(string code, string currentUser)
        {

            if (code.EqualsIgnoreCaseAndBlank("InBox"))
                return CountInbox(currentUser);
            if (code.EqualsIgnoreCaseAndBlank("Pending"))
                return CountPending(currentUser);
            if (code.EqualsIgnoreCaseAndBlank("Notification"))
                return CountNotifications(currentUser);
            if (code.EqualsIgnoreCaseAndBlank("Draft"))
                return CountDraft(currentUser);
            return null;
        }

        private int? CountInbox(string currentUser)
        {
            using (WorkFlowEntities entities = new WorkFlowEntities())
            {
                var user = new ApplicationUser(entities, currentUser);
                return user.CountInbox();
            }
        }

        private int? CountPending(string currentUser)
        {
            using (WorkFlowEntities entities = new WorkFlowEntities())
            {
                var user = new ApplicationUser(entities, currentUser);
                return user.CountPending();
            }
        }

        private int? CountNotifications(string currentUser)
        {
            using (WorkFlowEntities entities = new WorkFlowEntities())
            {
                var user = new ApplicationUser(entities, currentUser);
                return user.CountNotifications();
            }
        }

        private int? CountDraft(string currentUser)
        {
            using (WorkFlowEntities entities = new WorkFlowEntities())
            {
                var user = new ApplicationUser(entities, currentUser);
                return user.CountDraft();
            }
        }
    }
}