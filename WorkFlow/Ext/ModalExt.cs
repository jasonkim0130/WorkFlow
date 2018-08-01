using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using System.Web.Mvc.Html;
using System.Web.Routing;

namespace WorkFlow.Ext
{
    public class ModalAjaxOptions : AjaxOptions
    {
        public string ModalTargetId { get; set; }
        public string Id { get; set; }
        public string @Class { get; set; }
        public string OnSuccessPara { get; set; }
    }

    public static class ModalExt
    {
        public static MvcForm BeginModalForm(this AjaxHelper ajaxHelper, string actionName, string controller, RouteValueDictionary route, ModalAjaxOptions ajaxOptions, object htmlAttributes)
        {
            RouteValueDictionary dic = new RouteValueDictionary(htmlAttributes);
            if (ajaxOptions.Class != null)
                dic.Add("class", ajaxOptions.Class);
            if (ajaxOptions.Id != null)
                dic.Add("id", ajaxOptions.Id);
            if (!string.IsNullOrWhiteSpace(ajaxOptions.OnSuccess))
                dic.Add("data-success", ajaxOptions.OnSuccess);
            if (!string.IsNullOrWhiteSpace(ajaxOptions.OnSuccessPara))
                dic.Add("data-success-para", ajaxOptions.OnSuccessPara);
            ajaxOptions.OnSuccess = "$.modalOnSuccess";
            string modalTarget = (ajaxOptions.ModalTargetId ?? ajaxOptions.UpdateTargetId);
            if (!string.IsNullOrWhiteSpace(modalTarget))
                dic.Add("data-modal-target", "#" + modalTarget);
            if (!string.IsNullOrWhiteSpace(ajaxOptions.UpdateTargetId))
                dic.Add("data-target", "#" + ajaxOptions.UpdateTargetId);
            ajaxOptions.UpdateTargetId = null;
            return ajaxHelper.BeginForm(actionName, controller, route, ajaxOptions, dic);
        }

        public static MvcForm BeginModalForm(this AjaxHelper ajaxHelper, string actionName, string controller, ModalAjaxOptions ajaxOptions)
        {
            return BeginModalForm(ajaxHelper, actionName, null, null, ajaxOptions, null);
        }

        public static MvcForm BeginModalForm(this AjaxHelper ajaxHelper, string actionName, ModalAjaxOptions ajaxOptions, object htmlAttributes)
        {
            return BeginModalForm(ajaxHelper, actionName, null, null, ajaxOptions, htmlAttributes);
        }

        public static MvcForm BeginModalForm(this AjaxHelper ajaxHelper, string actionName, ModalAjaxOptions ajaxOptions)
        {
            return BeginModalForm(ajaxHelper, actionName, ajaxOptions, null);
        }

        public static MvcForm BeginModalForm(this AjaxHelper ajaxHelper, string actionName, object route, ModalAjaxOptions ajaxOptions, object htmlAttributes)
        {
            return BeginModalForm(ajaxHelper, actionName, null, new RouteValueDictionary(route), ajaxOptions, htmlAttributes);
        }

        public static ActionResult ModalView(this Controller controller, string view)
        {
            return new ViewResult { MasterName = "~/Views/Shared/_ModalLayout.cshtml", ViewName = view, ViewData = controller.ViewData };
        }

        public static ActionResult ModalView(this Controller controller, string view, object model)
        {
            controller.ViewData.Model = model;
            return new ViewResult { MasterName = "~/Views/Shared/_ModalLayout.cshtml", ViewName = view, ViewData = controller.ViewData };
        }

        public static ActionResult LargeModalView(this Controller controller, string view, object modal)
        {
            controller.ViewData.Model = modal;
            return new ViewResult { MasterName = "~/Views/Shared/_LargeModalLayout.cshtml", ViewName = view, ViewData = controller.ViewData };
        }

        public static ActionResult LargeModalView(this Controller controller, string view)
        {
            return new ViewResult { MasterName = "~/Views/Shared/_LargeModalLayout.cshtml", ViewName = view, ViewData = controller.ViewData };
        }

        public static ActionResult CloseModalView(this Controller controller, object value = null)
        {
            return new JsonResult { JsonRequestBehavior = JsonRequestBehavior.AllowGet, Data = new { close = true, data = value } };
        }

        public static ActionResult CloseModalView(this Controller controller, string scriptView, object model)
        {
            if (model != null)
                controller.ViewData.Model = model;
            return new PartialViewResult { ViewName = scriptView, ViewData = controller.ViewData };
        }

        public static ActionResult ShowErrorInModal(this Controller controller, string error)
        {
            return new JsonResult { JsonRequestBehavior = JsonRequestBehavior.AllowGet, Data = new { error = error } };
        }

        public static ActionResult ShowErrorModal(this Controller controller, string error)
        {
            controller.ViewData.Model = error;
            controller.ViewData["DisplayButtons"] = false;
            return new ViewResult { MasterName = "~/Views/Shared/_ModalLayout.cshtml", ViewName = "~/Views/Shared/_PartialError.cshtml", ViewData = controller.ViewData };
        }

        public static ActionResult ShowErrorModal(this Controller controller, string viewName, object model)
        {
            controller.ViewData.Model = model;
            controller.ViewData["DisplayButtons"] = false;
            return new ViewResult { MasterName = "~/Views/Shared/_ModalLayout.cshtml", ViewName = viewName, ViewData = controller.ViewData };
        }

        public static ActionResult ShowSuccessModal(this Controller controller, string info)
        {
            controller.ViewData.Model = info;
            controller.ViewData["DisplayButtons"] = false;
            return new ViewResult { MasterName = "~/Views/Shared/_ModalLayout.cshtml", ViewName = "~/Views/Shared/_PartialSuccess.cshtml", ViewData = controller.ViewData };
        }

        public static ActionResult ShowSuccessModal(this Controller controller, string viewName, object model)
        {
            controller.ViewData.Model = model;
            controller.ViewData["DisplayButtons"] = false;
            return new ViewResult { MasterName = "~/Views/Shared/_ModalLayout.cshtml", ViewName = viewName, ViewData = controller.ViewData };
        }

        public static MvcHtmlString ModalActionLink(this AjaxHelper ajaxHelper, string linkText, string actionName,
            object routeValues, ModalAjaxOptions ajaxOptions, object htmlAttributes)
        {
            RouteValueDictionary dic = new RouteValueDictionary(htmlAttributes);
            if (ajaxOptions.Class != null)
                dic.Add("class", ajaxOptions.Class);
            if (ajaxOptions.Id != null)
                dic.Add("id", ajaxOptions.Id);
            if (!string.IsNullOrWhiteSpace(ajaxOptions.OnSuccess))
                dic.Add("data-success", ajaxOptions.OnSuccess);
            if (!string.IsNullOrWhiteSpace(ajaxOptions.OnSuccessPara))
                dic.Add("data-success-para", ajaxOptions.OnSuccessPara);
            ajaxOptions.OnSuccess = "$.modalOnSuccess";
            string modalTarget = (ajaxOptions.ModalTargetId ?? ajaxOptions.UpdateTargetId);
            if (!string.IsNullOrWhiteSpace(modalTarget))
                dic.Add("data-modal-target", "#" + modalTarget);
            if (!string.IsNullOrWhiteSpace(ajaxOptions.UpdateTargetId))
                dic.Add("data-target", "#" + ajaxOptions.UpdateTargetId);
            ajaxOptions.UpdateTargetId = null;
            return ajaxHelper.ActionLink(linkText, actionName, new RouteValueDictionary(routeValues), ajaxOptions, dic);
        }

        public static MvcHtmlString ModalActionLink(this AjaxHelper ajaxHelper, string linkText, string actionName,
          object routeValues, ModalAjaxOptions ajaxOptions)
        {
            return ModalActionLink(ajaxHelper, linkText, actionName, routeValues, ajaxOptions, null);
        }
    }
}