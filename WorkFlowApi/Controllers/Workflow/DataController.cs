using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Dreamlab.Core;
using Unity;
using WorkFlowLib;
using WorkFlowLib.Data;
using WorkFlowLib.Parameters;
using WorkFlowLib.DTO;
using Omnibackend.Api.Workflow.Data;
using Omnibackend.Api.Workflow.Logic;

namespace Omnibackend.Api.Controllers.Workflow
{
    [RoutePrefix("workflow/data")]
    [Authorize]
    public class DataController : BaseWFApiController
    {
        [AllowAnonymous]
        [Route("version")]
        [HttpGet]
        [HttpPost]
        public string version()
        {
            return "1.0";
        }

        [Route("get_user_list")]
        [HttpGet]
        [HttpPost]
        public object get_user_list()
        {
            return Entities.GlobalUserView.Where(p => p.EmpStatus == "A").Select(p => new
            {
                userno = p.EmployeeID,
                username = p.EmployeeName
            }).DistinctBy(p => p.userno);
        }

        [Route("user_list_by_count")]
        [HttpGet]
        public object get_user_list(int clientCount)
        {
            int cnt = 0;
            try
            {
                cnt = Entities.GlobalUserView.Where(p => p.EmpStatus == "A").Count();
            }
            catch (Exception e)
            {
                return "[]";
            }

            if (cnt != clientCount)
            {
                return Entities.GlobalUserView.Where(p => p.EmpStatus == "A").Select(p => new
                {
                    userno = p.EmployeeID,
                    username = p.EmployeeName
                }).DistinctBy(p => p.userno);
            }

            return "[]";
        }

        [Route("get_application_type_list")]
        [HttpGet]
        [HttpPost]
        public object get_application_type_list(string lang = "en_US")
        {
            var uds = Entities.UserDeviceTokens.Where(p => p.UserNo == User.Identity.Name && p.StatusId > 0).ToList();
            foreach (var ud in uds)
            {
                ud.LastDeviceLang = lang;
            }

            Entities.SaveChanges();
            var query = Entities.WF_FlowTypes.Where(p => p.StatusId > 0);
            if (!Codehelper.IsUat)
                query = query.Where(p => p.TemplateType == 2);
            var data = query.Select(p => new FlowTypeDTO
            {
                templateId = p.FlowTypeId,
                templateTypeId = p.TemplateType,
                templateCode = p.Name,
                templateName = p.Name,
                IconUrl = p.IconUrl,
                Tabs = p.Tabs
            }).ToArray();
            lang = lang.Replace('_', '-');
            string[] acceptlangs = new[] { "zh-CN", "zh-TW", "en-US", "ko-KR" };
            if (acceptlangs.Any(p => p.EqualsIgnoreCaseAndBlank(lang)))
            {
                Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture = new CultureInfo(lang);
                foreach (var item in data)
                {
                    item.templateName = item.templateCode.ToLocal();
                }
            }

            return data;
        }

        [Obsolete]
        [Route("get_properties")]
        [HttpGet]
        public object get_properties([FromUri] int maxPropertyId, [FromUri] string templateTypeIds = null)
        {
            List<int> typeids = new List<int>();
            if (templateTypeIds != null)
            {
                string[] tmp = templateTypeIds.Split(',');
                foreach (string strId in tmp)
                {
                    typeids.Add(int.Parse(strId));
                }
            }

            int? max = 0;
            System.Linq.Expressions.Expression<Func<WF_FlowPropertys, bool>> exp = null;
            if (templateTypeIds != null)
            {
                var flowTypeIds = Entities.WF_FlowTypes
                    .Where(ft => ft.StatusId > 0 && ft.TemplateType != null && typeids.Contains(ft.TemplateType.Value))
                    .Select(ft => ft.FlowTypeId);

                var groupIds = Entities.WF_FlowGroups
                    .Where(fg => fg.StatusId > 0 && flowTypeIds.Contains(fg.FlowTypeId)).Select(fg => fg.FlowGroupId);

                exp = fp => (fp.StatusId < 0 || fp.StatusId > 0) &&
                            fp.FlowGroupId != null && groupIds.Contains(fp.FlowGroupId.Value);
                max = Entities.WF_FlowPropertys
                    .Where(fp => fp.StatusId != 0 && fp.FlowGroupId != null && groupIds.Contains(fp.FlowGroupId.Value))
                    .Max(fp => fp.FlowPropertyId);
            }
            else
            {
                exp = fp => (fp.StatusId < 0 || fp.StatusId > 0) && fp.WF_FlowGroups.StatusId > 0;
                max = Entities.WF_FlowPropertys.Where(fp => fp.StatusId != 0).Max(fp => fp.FlowPropertyId);
            }

            if (max.HasValue && max > maxPropertyId)
            {
                var data = Entities.WF_FlowPropertys.Where(exp).Select(fp => new
                {
                    FlowTypeId = fp.WF_FlowGroups.FlowTypeId,
                    PropertyId = fp.FlowPropertyId,
                    PropertyType = fp.PropertyType,
                    IsRequired = fp.Compulsory,
                    PropertyName = fp.PropertyName,
                    Tab = fp.Tab,
                    RowIndex = fp.RowIndex,
                    ColumnIndex = fp.ColumnIndex,
                    ViewType = fp.ViewType,
                    Text = fp.Text,
                    FontSize = fp.FontSize,
                    FontColor = fp.FontColor,
                    Width = fp.Width,
                    Height = fp.Height,
                    HAlign = fp.HAlign,
                    VAlign = fp.VAlign,
                    DataSource = fp.DataSource,
                    Multiple = fp.AllowMultiple,
                    FieldType = fp.PropertyType,
                    Validation = fp.Validation,
                    Compulsory = fp.Compulsory,
                    BgColor = fp.BgColor,
                    ValidationMsg = fp.ValidationMsg
                }).ToArray();
                return data;
            }

            return "[]";
        }

        [Route("v2/get_properties")]
        [HttpGet]
        public object get_properties_v2([FromUri] long? lastUpdated = null,
            [FromUri] string templateTypeIds = null)
        {
            List<int?> typeids = new List<int?>();
            if (templateTypeIds != null)
            {
                string[] tmp = templateTypeIds.Split(',');
                foreach (string strId in tmp)
                {
                    typeids.Add(int.Parse(strId));
                }
            }

            var query = Entities.WF_FlowPropertys.Where(p => p.WF_FlowGroups.StatusId > 0 && p.WF_FlowGroups.WF_FlowTypes.StatusId > 0);

            if (typeids.Count > 0)
            {
                query = query.Where(p => typeids.Contains(p.WF_FlowGroups.WF_FlowTypes.TemplateType));
            }

            DateTime? max = query.Max(p => (DateTime?)p.LastUpdated);

            if (max.HasValue && lastUpdated.HasValue)
            {
                if (lastUpdated.Value >= max.Value.Ticks)
                {
                    return new
                    {
                        lastUpdated = max.Value.Ticks,
                        data = new object[] { }
                    };
                }
            }

            return new
            {
                lastUpdated = max?.Ticks ?? 0,
                data = query.Where(p => p.StatusId != 0).Select(fp => new
                {
                    FlowTypeId = fp.WF_FlowGroups.FlowTypeId,
                    Property = fp
                }).ToArray().Select(p => new
                {
                    FlowTypeId = p.FlowTypeId,
                    PropertyId = p.Property.FlowPropertyId,
                    PropertyType = p.Property.PropertyType,
                    IsRequired = p.Property.Compulsory,
                    PropertyName = p.Property.PropertyName,
                    Tab = p.Property.Tab,
                    RowIndex = p.Property.RowIndex,
                    ColumnIndex = p.Property.ColumnIndex,
                    ViewType = p.Property.ViewType,
                    Text = p.Property.Text,
                    FontSize = p.Property.FontSize,
                    FontColor = p.Property.FontColor,
                    Width = p.Property.Width,
                    Height = p.Property.Height,
                    HAlign = p.Property.HAlign,
                    VAlign = p.Property.VAlign,
                    DataSource = p.Property.DataSource,
                    Multiple = p.Property.AllowMultiple,
                    FieldType = p.Property.PropertyType,
                    Validation = p.Property.Validation,
                    Compulsory = p.Property.Compulsory,
                    BgColor = p.Property.BgColor,
                    ValidationMsg = p.Property.ValidationMsg,
                    LastUpdated = p.Property.LastUpdated.Ticks
                })
            };
        }

        [Route("get_charge_departments")]
        [HttpGet]
        [AllowAnonymous]
        public object get_charge_departments()
        {
            using (ApiClient client = new ApiUserManager().CreateApiClient())
            {
                List<object> items = new List<object>();
                foreach (string country in new[] { "CHN", "TWN", "KOR", "MYS", "SGP", "HKG" })
                {
                    var result = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = country,
                        id = "ZADEPT1",
                        language = "ENG",
                        code = "%"
                    });
                    if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    {
                        return new { ret_code = RetCode.Error, ret_msg = "ERROR FROM API:" + result.ErrorMessage };
                    }

                    items.AddRange(result.ReturnValue.Select(r => new
                    {
                        Country = country,
                        DeptCode = r.ZZ03_CODE,
                        DeptDesc = r.ZZ03_CDC1
                    }));
                }

                return items;
            }
        }

        [Route("get_all_charge_departments")]
        [HttpGet]
        [AllowAnonymous]
        public object get_all_charge_departments()
        {
            using (ApiClient client = new ApiUserManager().CreateApiClient())
            {
                var result = client.MasterCode_Search(new MasterCodeSearchParams
                {
                    country = Consts.GetApiCountry(),
                    id = "ZADEPT1",
                    language = "ENG",
                    code = "%"
                });
                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                {
                    var ret = result.ReturnValue.Select(r => new
                    {
                        DeptCode = r.ZZ03_CODE,
                        DeptDesc = r.ZZ03_CDC1
                    });
                    return ret.ToArray();
                }

                return new { ret_code = RetCode.Error, ret_msg = "ERROR FROM API:" + result.ErrorMessage };
            }
        }

        [Route("get_holidays")]
        [HttpPost]
        public object get_holidays(HolidayModel model)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-get_holidays", JsonConvert.SerializeObject(model));
            using (ApiClient client = new ApiUserManager().CreateApiClient())
            {
                var result = client.Get_Holiday(new HolidayParameter
                {
                    AS_CUTY = model.country_code,
                    AS_FRDT = model.from_date,
                    AS_TODT = model.to_date
                });
                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                {
                    return result.ReturnValue.Select(p => new
                    {
                        date = p.DATE,
                        time = p.TIME,
                        remark = p.REMARK
                    }).ToArray();
                }

                return null;
            }
        }

        [Route("check_version")]
        [HttpPost]
        [HttpGet]
        [AllowAnonymous]
        public object check_version([FromUri] string country, [FromUri] string appId, [FromUri] string version)
        {
            string stat = null;
            string error = null;
            try
            {
                using (LoginApiClient client =
                    new LoginApiClient(Codehelper.Get3011ApiUrl(country)))
                {
                    string token = ConfigurationManager.AppSettings["LoginToken"];
                    client.Wrapper.Client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                    RequestResult<string> checkVersion = client.Wrapper.PostString("tablet/AppVer_CHK_V2", new
                    {
                        AS_CUTY = country,
                        AS_APID = appId,
                        AS_APVR = version
                    });
                    if (!string.IsNullOrWhiteSpace(checkVersion.ErrorMessage))
                    {
                        error = checkVersion.ErrorMessage;
                    }
                    else
                    {
                        JObject root = JObject.Parse(checkVersion.ReturnValue);
                        stat = root["data"].Value<string>("RET_STAT");
                        if (stat.EqualsIgnoreCaseAndBlank("U"))
                        {
                            RequestResult<string> getApp = client.Wrapper.PostString("tablet/AppAccess_List_V1", new
                            {
                                AS_USID = User.Identity.Name ?? "2298311094",
                            });
                            if (!string.IsNullOrWhiteSpace(getApp.ErrorMessage))
                            {
                                error = getApp.ErrorMessage;
                            }
                            else
                            {
                                JObject root2 = JObject.Parse(getApp.ReturnValue);
                                var data = (JArray)root2["data"];
                                var item = data.FirstOrDefault(p =>
                                    p.Value<string>("APP_NAME").EqualsIgnoreCaseAndBlank(appId));
                                if (item != null)
                                {
                                    return new
                                    {
                                        version = item.Value<string>("VERSION"),
                                        forceUpdate = stat.EqualsIgnoreCaseAndBlank("U"),
                                        apk = item.Value<string>("APK"),
                                        url = item.Value<string>("PATH") + "/" + item.Value<string>("APK")
                                    };
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return new
            {
                error = error ?? "Unable to check version"
            };
        }

        [Route("check_new_messages")]
        [HttpPost]
        public object check_new_messages(CheckNewMsgModel model)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-check_new_messages", JsonConvert.SerializeObject(model));
            ApplicationUser user = new ApplicationUser(Entities, User.Identity.Name);
            return new
            {
                inbox = user.InboxQuery
                    .Where(p => !model.inboxList.Contains(p.CaseUserActionId))
                    .Select(p => new
                    {
                        actionId = p.CaseUserActionId,
                        flowCaseId = p.FlowCaseId,
                        department = p.WF_FlowCases.Department,
                        applicant = p.WF_FlowCases.UserNo,
                        templateName = p.WF_FlowCases.WF_Flows.WF_FlowGroups.WF_FlowTypes.Name,
                        subject = p.WF_FlowCases.Subject,
                        ver = p.WF_FlowCases.Ver ?? 0,
                        received = p.Created,
                        deadline = p.WF_FlowCases.Deadline,
                        isFlagged = p.WF_FlowCases.IsFlagged,
                        isReadByApprover = p.StatusId == 2 ? 1 : 0,
                        attachments = p.WF_FlowCases.WF_FlowCases_Attachments.Where(q => q.StatusId > 0).Select(q => new
                        {
                            attachmentId = q.AttachementId,
                            fileName = q.OriFileName
                        }),
                        lastUpdated = p.LastUpdated
                    }),
                notification =
                user.GetUndismissedNotifications()
                    .Where(p => !model.notificationList.Contains(p.CaseNotificationReceiverId)).ToArray()
                    .Select(p => new
                    {
                        notificationId = p.CaseNotificationReceiverId,
                        notificationType = p.NotificationType,
                        flowCaseId = p.FlowCaseId,
                        department = p.Department,
                        applicant = p.Applicant,
                        templateName = p.Type,
                        subject = p.Subject,
                        ver = p.Ver ?? 0,
                        received = p.Created,
                        deadline = p.Deadline,
                        sourceType = p.SourceType,
                        sender = p.Sender,
                        attachments =
                        Entities.WF_FlowCases_Attachments.Where(
                                q => q.StatusId > 0 && q.FlowCaseId == p.FlowCaseId)
                            .Select(q => new
                            {
                                attachmentId = q.AttachementId,
                                fileName = q.OriFileName
                            })
                    })
            };
        }

        [Route("get_inbox_list")]
        [HttpGet]
        [HttpPost]
        public object get_inbox_list()
        {
            ApplicationUser user = new ApplicationUser(Entities, User.Identity.Name);
            return user.InboxQuery.OrderByDescending(p => p.Created).Select(p => new
            {
                actionId = p.CaseUserActionId,
                flowCaseId = p.FlowCaseId,
                department = p.WF_FlowCases.Department,
                applicant = p.WF_FlowCases.UserNo,
                templateName = p.WF_FlowCases.WF_Flows.WF_FlowGroups.WF_FlowTypes.Name,
                templateType = p.WF_FlowCases.WF_Flows.WF_FlowGroups.WF_FlowTypes.TemplateType,
                subject = p.WF_FlowCases.Subject,
                templateId = p.WF_FlowCases.WF_Flows.WF_FlowGroups.WF_FlowTypes.FlowTypeId,
                ver = p.WF_FlowCases.Ver ?? 0,
                received = p.Created,
                deadline = p.WF_FlowCases.Deadline,
                isFlagged = p.WF_FlowCases.IsFlagged,
                isFinished = p.WF_FlowCases.ReviseAbort != null,
                isReadByApprover = p.StatusId == 2 ? 1 : 0,
                attachments = p.WF_FlowCases.WF_FlowCases_Attachments.Where(q => q.StatusId > 0).Select(q => new
                {
                    attachmentId = q.AttachementId,
                    fileName = q.OriFileName,
                    dbFileName = q.FileName
                }),
                lastUpdated = p.LastUpdated
            });
        }

        [Route("get_notifications")]
        [HttpGet]
        public object get_notifications()
        {
            ApplicationUser user = new ApplicationUser(Entities, User.Identity.Name);
            var notifications = user.GetUndismissedNotifications().ToArray();
            return notifications.Select(p => new
            {
                notificationId = p.CaseNotificationReceiverId,
                notificationType = p.NotificationType,
                flowCaseId = p.FlowCaseId,
                department = p.Department,
                applicant = p.Applicant,
                templateName = p.Type,
                templateType = Entities.WF_Flows.FirstOrDefault(q => q.FlowId == p.FlowId)?.WF_FlowGroups.WF_FlowTypes
                    .TemplateType,
                subject = p.Subject,
                ver = p.Ver ?? 0,
                received = p.Created,
                deadline = p.Deadline,
                sourceType = p.SourceType,
                sender = p.Sender,
                attachments =
                Entities.WF_FlowCases_Attachments.Where(q => q.StatusId > 0 && q.FlowCaseId == p.FlowCaseId)
                    .Select(q => new
                    {
                        attachmentId = q.AttachementId,
                        fileName = q.OriFileName,
                        dbFileName = q.FileName
                    })
            });
        }

        [Route("get_pending_applications")]
        [HttpGet]
        public object get_pending_applications()
        {
            ApplicationUser manager = new ApplicationUser(Entities, User.Identity.Name);
            return manager.PendingQuery
                .Select(p => new
                {
                    flowCaseId = p.FlowCaseId,
                    department = p.Department,
                    applicant = p.UserNo,
                    templateName = p.WF_Flows.WF_FlowGroups.WF_FlowTypes.Name,
                    templateType = p.WF_Flows.WF_FlowGroups.WF_FlowTypes.TemplateType,
                    //templateId = p.WF_Flows.WF_FlowGroups.WF_FlowTypes.FlowTypeId,
                    received = p.Created,
                    subject = p.Subject,
                    ver = p.Ver,
                    completed =
                    p.WF_Flows.WF_StepGroups.Count(q => q.StatusId > 0 && q.OrderId <= p.WF_StepGroups.OrderId),
                    total = p.WF_Flows.WF_StepGroups.Count(q => q.StatusId > 0),
                    isFinished = p.ReviseAbort != null,
                    attachments = p.WF_FlowCases_Attachments.Where(q => q.StatusId > 0).Select(q => new
                    {
                        attachmentId = q.AttachementId,
                        fileName = q.OriFileName,
                        dbFileName = q.FileName
                    }),
                }).ToArray();
        }

        [Route("get_archives")]
        [HttpGet]
        public object get_archive_applications()
        {
            ApplicationUser manager = new ApplicationUser(Entities, User.Identity.Name);
            var myNotifiedApplications = manager.GetDismissedNotifications().ToArray();
            return myNotifiedApplications
                .Select(p => new
                {
                    notificationId = p.CaseNotificationReceiverId,
                    notificationType = p.NotificationType,
                    flowCaseId = p.FlowCaseId,
                    department = p.Department,
                    applicant = p.Applicant,
                    templateName = p.Type,
                    templateType = Entities.WF_Flows.FirstOrDefault(q => q.FlowId == p.FlowId)?.WF_FlowGroups
                        .WF_FlowTypes.TemplateType,
                    subject = p.Subject,
                    ver = p.Ver ?? 0,
                    received = p.Created,
                    deadline = p.Deadline,
                    sourceType = p.SourceType,
                    sender = p.Sender,
                    attachments =
                    Entities.WF_FlowCases_Attachments.Where(q => q.StatusId > 0 && q.FlowCaseId == p.FlowCaseId)
                        .Select(q => new
                        {
                            attachmentId = q.AttachementId,
                            fileName = q.OriFileName,
                            dbFileName = q.FileName
                        })
                });
        }

        [Route("get_draft")]
        [HttpGet]
        public object get_draft_applications()
        {
            ApplicationUser manager = new ApplicationUser(Entities, User.Identity.Name);
            return manager.DraftQuery.Where(p => p.WF_Flows.WF_FlowGroups.WF_FlowTypes.TemplateType == 4)
                .Select(p => new
                {
                    flowCaseId = p.FlowCaseId,
                    department = p.Department,
                    applicant = p.UserNo,
                    templateName = p.WF_Flows.WF_FlowGroups.WF_FlowTypes.Name,
                    templateType = p.WF_Flows.WF_FlowGroups.WF_FlowTypes.TemplateType,
                    templateId = p.WF_Flows.WF_FlowGroups.WF_FlowTypes.FlowTypeId,
                    received = p.Created,
                    subject = p.Subject,
                    ver = p.Ver,
                    completed =
                    p.WF_Flows.WF_StepGroups.Count(q => q.StatusId > 0 && q.OrderId <= p.WF_StepGroups.OrderId),
                    total = p.WF_Flows.WF_StepGroups.Count(q => q.StatusId > 0),
                    isFinished = p.ReviseAbort != null,
                    attachments = p.WF_FlowCases_Attachments.Where(q => q.StatusId > 0).Select(q => new
                    {
                        attachmentId = q.AttachementId,
                        fileName = q.OriFileName,
                        dbFileName = q.FileName
                    }),
                }).ToArray();
        }

        [Route("get_approved_overseatrips")]
        [HttpPost]
        //[AllowAnonymous]
        public object get_approved_overseatrips(PropertiesModel model)
        {
            string[] properties = model.properties;
            string username = User.Identity.Name;
            var query = from item in Entities.WF_FlowCases
                        let ftype = item.WF_Flows.WF_FlowGroups.WF_FlowTypes
                        where item.Approved != null
                              && (model.flowCaseId.HasValue ||
                                  !Entities.WF_FlowCases.Where(p => p.StatusId > 0).Select(p => p.RelatedFlowCaseId)
                                      .Contains(item.FlowCaseId))
                              && item.UserNo == username
                              && ftype.StatusId > 0
                              && ftype.TemplateType == 3
                              && item.StatusId > 0
                        select new
                        {
                            flowCaseId = item.FlowCaseId,
                            propertyValues = from value in item.WF_CasePropertyValues
                                             let prop = value.WF_FlowPropertys
                                             where properties.Contains(prop.PropertyName)
                                             select new
                                             {
                                                 name = prop.PropertyName,
                                                 type = prop.PropertyType,
                                                 code = prop.WF_TemplatePropCode.PropCode,
                                                 stringValue = value.StringValue ?? value.TextValue,
                                                 intValue = value.IntValue,
                                                 dateTimeValue = value.DateTimeValue,
                                                 numericValue = value.NumericValue,
                                                 dateValue = value.DateValue
                                             }
                        };
            if (model.flowCaseId.HasValue)
                query = query.Where(p => p.flowCaseId == model.flowCaseId);
            return query.ToArray();
        }

        [Route("get_all_flowcases")]
        [HttpPost]
        public object get_all_flowcases(GetAllFlowCaseModel models)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-get_all_flowcases", JsonConvert.SerializeObject(models));
            ApplicationUser user = new ApplicationUser(Entities, User.Identity.Name);
            int[] flowCaseIds =
                user.GetAllRelatedFlowCaseId()
                    .Where(
                        p =>
                            models.pairs.All(q => q.flowCaseId != p.flowCaseId) ||
                            models.pairs.First(q => q.flowCaseId == p.flowCaseId).lastUpdated < p.lastUpdated)
                    .Select(p => p.flowCaseId).Distinct().ToArray();
            var result = new
            {
                total = flowCaseIds.Length,
                data = from item in flowCaseIds.OrderBy(p => p).Skip(models.currentPage * 50).Take(50)
                       let flowinfo = user.GetFlowAndCase(item)
                       let props = user.GetProperties(item)
                       select
                       (flowinfo == null ||
                        (models.pairs.Any(p => p.flowCaseId == item) &&
                         flowinfo.CaseInfo.LastUpdated <= models.pairs.First(p => p.flowCaseId == item).lastUpdated))
                           ? null
                           : new
                           {
                               flowCaseId = item,
                               lastUpdated = flowinfo.CaseInfo.LastUpdated.ToString("yyyy-MM-dd'T'HH:mm:ss.fff"),
                               subject = flowinfo.CaseInfo.Subject,
                               deadline = flowinfo.CaseInfo.Deadline,
                               dep = flowinfo.CaseInfo.Department,
                               lastChecked = flowinfo.CaseInfo.LastChecked,
                               stepGroups = flowinfo.StepGroups.Select(q => new
                               {
                                   groupId = q.StepGroupId,
                                   conditionId = q.StepConditionId,
                                   orderId = q.OrderId,
                                   status = q.StepStatus,
                                   users = q.NotificationUsers.Select(s => s.UserNo),
                                   steps = q.Steps.Select(s => new
                                   {
                                       stepId = s.FlowStepId,
                                       approver = s.GetApprover(),
                                       finalApprover = s.FinalApprover,
                                       finalDep = s.FinalDepartment,
                                       status = s.StepStatus,
                                       dep = s.Department
                                   }),
                               }),
                               notifyUsers = Entities.WF_CaseNotificateUsers.Where(q => q.FlowCaseId == item && q.StatusId > 0)
                                   .Select(q => q.UserNo),
                               coverDuties = Entities.WF_CaseCoverUsers.Where(q => q.FlowCaseId == item && q.StatusId > 0)
                                   .Select(q => q.UserNo),
                               caseLogs = user.GetCaseLogs(item).Union(user.GetCaseNotificationLogs(item))
                                   .OrderBy(q => q.Created),
                               fields = props != null
                                   ? (from prop in props.PropertyInfo
                                      let value =
                                   props.Values?.FirstOrDefault(q => q.PropertyId == prop.FlowPropertyId)
                                      select new
                                      {
                                          name = prop.PropertyName,
                                          type = prop.PropertyType,
                                          code = prop.WF_TemplatePropCode?.PropCode,
                                          value = value == null
                                       ? null
                                       : value.StringValue ??
                                         value.IntValue?.ToString() ??
                                         value.DateTimeValue?.ToString("yyyy-MM-dd'T'HH:mm:ss.fff") ??
                                         value.NumericValue?.ToString("f2") ??
                                         value.DateValue?.ToString("yyyy-MM-dd'T'HH:mm:ss.fff") ??
                                         value.UserNoValue ?? value.TextValue,
                                          orderid = prop.OrderId ?? 0
                                      })
                                   : null,
                               attachments =
                               Entities.WF_FlowCases_Attachments.Where(
                                       p => p.FlowCaseId == item && p.StatusId > 0)
                                   .Select(p => new
                                   {
                                       attachmentId = p.AttachementId,
                                       fileName = p.OriFileName,
                                       dbFileName = p.FileName,
                                       fileSize = p.FileSize,
                                       lastUpdated = p.LastUpdated
                                   })
                           }
            };
            return result;
        }

        [Route("get_caselog")]
        [HttpGet]
        public object get_caselog(int flowCaseId)
        {
            ApplicationUser user = new ApplicationUser(Entities, User.Identity.Name);
            return user.GetCaseLogs(flowCaseId).Union(user.GetCaseNotificationLogs(flowCaseId)).OrderBy(q => q.Created);
        }

        [Route("get_flowcase_fullinfo")]
        [HttpGet]
        [HttpPost]
        public object get_flowcase_fullinfo(int flowCaseId)
        {
            ApplicationUser user = new ApplicationUser(Entities, User.Identity.Name);
            var flowinfo = user.GetFlowAndCase(flowCaseId);
            if (flowinfo == null)
                return null;
            var properties = user.GetProperties(flowCaseId);
            user.SetCaseAsViewed(flowCaseId, User.Identity.Name);
            user.UpdateLastChecked(flowCaseId, User.Identity.Name);
            return new
            {
                flowCaseId,
                lastUpdated = flowinfo.CaseInfo.LastUpdated.ToString("yyyy-MM-dd'T'HH:mm:ss.fff"),
                subject = flowinfo.CaseInfo.Subject,
                deadline = flowinfo.CaseInfo.Deadline,
                dep = flowinfo.CaseInfo.Department,
                lastChecked = flowinfo.CaseInfo.LastChecked,
                stepGroups = flowinfo.StepGroups.Select(q => new
                {
                    groupId = q.StepGroupId,
                    conditionId = q.StepConditionId,
                    orderId = q.OrderId,
                    status = q.StepStatus,
                    users = q.NotificationUsers.Select(s => s.UserNo),
                    steps = q.Steps.Select(s => new
                    {
                        stepId = s.FlowStepId,
                        approver = s.GetApprover(),
                        finalApprover = s.FinalApprover,
                        finalDep = s.FinalDepartment,
                        status = s.StepStatus,
                        dep = s.Department
                    }),
                }),
                notifyUsers = Entities.WF_CaseNotificateUsers.Where(q => q.FlowCaseId == flowCaseId && q.StatusId > 0)
                    .Select(q => q.UserNo),
                coverDuties = Entities.WF_CaseCoverUsers.Where(q => q.FlowCaseId == flowCaseId && q.StatusId > 0)
                    .Select(q => q.UserNo),
                caseLogs = user.GetCaseLogs(flowCaseId).Union(user.GetCaseNotificationLogs(flowCaseId))
                    .OrderBy(q => q.Created),
                fields = properties != null
                    ? (from item in properties.PropertyInfo
                       let value = properties.Values?.FirstOrDefault(q => q.PropertyId == item.FlowPropertyId)
                       select new
                       {
                           name = item.PropertyName,
                           type = item.PropertyType,
                           code = item.WF_TemplatePropCode?.PropCode,
                           value = value == null
                               ? null
                               : value.StringValue ??
                                 value.IntValue?.ToString() ??
                                 value.DateTimeValue?.ToString("yyyy-MM-dd'T'HH:mm:ss.fff") ??
                                 value.NumericValue?.ToString("f2") ??
                                 value.DateValue?.ToString("yyyy-MM-dd'T'HH:mm:ss.fff") ??
                                 value.UserNoValue ?? value.TextValue,
                           orderid = item.OrderId ?? 0
                       })
                    : null,
                attachments =
                Entities.WF_FlowCases_Attachments.Where(p => p.FlowCaseId == flowCaseId && p.StatusId > 0)
                    .Select(p => new
                    {
                        attachmentId = p.AttachementId,
                        fileName = p.OriFileName,
                        fileSize = p.FileSize,
                        dbFileName = p.FileName,
                        lastUpdated = p.LastUpdated
                    })
            };
        }

        private static string GetContentType(string filename)
        {
            string ext = Path.GetExtension(filename);
            var imgs = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            if (imgs.Any(p => p.EqualsIgnoreCaseAndBlank(ext)))
            {
                return "image/" + ext.Trim('.');
            }

            var txts = new[] { ".txt", ".xml" };
            if (txts.Any(p => p.EqualsIgnoreCaseAndBlank(ext)))
            {
                return "text/" + ext.Trim('.');
                ;
            }

            if (ext.EqualsIgnoreCaseAndBlank(".pdf"))
            {
                return "application/pdf";
            }

            var ppt = new[] { ".ppt", ".pptx" };
            if (ppt.Any(p => p.EqualsIgnoreCaseAndBlank(ext)))
            {
                return "application/ms-powerpoint";
            }

            var excel = new[] { ".xls", ".xlsx" };
            if (excel.Any(p => p.EqualsIgnoreCaseAndBlank(ext)))
            {
                return "application/ms-excel";
            }

            return "application/octet-stream";
        }

        [AllowAnonymous]
        [Route("download_attachment")]
        [HttpGet]
        public HttpResponseMessage download_attachment(int attachmentId)
        {
            string token = Request.Headers.GetValues("clientsecret").FirstOrDefault();
            if (!token.Equals(Consts.WorkFlowApIToken))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            WF_FlowCases_Attachments target =
                Entities.WF_FlowCases_Attachments.FirstOrDefault(p =>
                    p.AttachementId == attachmentId && p.StatusId > 0);
            if (target == null)
                return new HttpResponseMessage(HttpStatusCode.NoContent) { ReasonPhrase = "File not existed." };
            string path = Path.Combine(Dir, target.FileName);
            if (!File.Exists(path))
                return new HttpResponseMessage(HttpStatusCode.NoContent) { ReasonPhrase = "File not existed." };
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = target.FileName
            };

            return response;
        }

        [AllowAnonymous]
        [Route("get_image_file")]
        [HttpGet]
        public HttpResponseMessage get_image_file(int attachmentId)
        {
            WF_FlowCases_Attachments target =
                Entities.WF_FlowCases_Attachments.FirstOrDefault(p =>
                    p.AttachementId == attachmentId && p.StatusId > 0);
            if (target == null)
                return new HttpResponseMessage(HttpStatusCode.NoContent) { ReasonPhrase = "File not existed." };
            string path = Path.Combine(Dir, target.FileName);
            if (!File.Exists(path))
                return new HttpResponseMessage(HttpStatusCode.NoContent) { ReasonPhrase = "File not existed." };
            string contentType = GetContentType(path);
            if (!contentType.StartsWith("image"))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            //response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            //{
            //    FileName = target.FileName
            //};

            return response;
        }

        [Route("refresh_lastchecked")]
        [HttpPost]
        public object refresh_lastchecked(LastCheckedModel model)
        {
            Singleton<ILogWritter>.Instance?.WriteLog("WorkFlow-refresh_lastchecked", JsonConvert.SerializeObject(model));
            var target = Entities.WF_FlowCases.FirstOrDefault(p => p.FlowCaseId == model.flowCaseId);
            if (target == null)
                return new { ret_code = RetCode.Error, ret_msg = "Invalid flowCaseId" };
            if (target.LastChecked >= model.lastChecked)
                return new { ret_code = RetCode.Error, ret_msg = "LastChecked need not be updated" };
            target.LastChecked = model.lastChecked;
            if (Entities.SaveChanges() > 0)
                return new { ret_code = RetCode.Success, ret_msg = string.Empty };
            return new { ret_code = RetCode.Failure, ret_msg = "Update failed" };
        }

        [Route("upload_app_error")]
        [HttpGet]
        public void upload_app_error(string error)
        {
            string sql = @"INSERT INTO dbo.SystemLogs
(
    Description,
    Created,
    StatusID,
    LogTypeId,
    LogTitle,
    Parameter
)
VALUES
(   
    N'WorkFlow App',       
    GETDATE(), 
    1,       
    NULL,       
    N'Exception',      
    @p0     
)";
            Entities.Database.ExecuteSqlCommandAsync(sql, error);
        }

        [Route("upload_registration")]
        [HttpGet]
        public object upload_registration(string registration)
        {
            var rows = Entities.UserDeviceTokens.Where(p => p.UserNo == User.Identity.Name && p.StatusId > 0).ToList();
            bool exist =
                Entities.UserDeviceTokens.Count(p =>
                    p.UserNo == User.Identity.Name && p.StatusId > 0 && p.DeviceToken.Equals(registration)) > 0
                    ? true
                    : false;
            //var target = _entities.Users.FirstOrDefault(p => p.UserNo == User.Identity.Name);
            if (!exist)
            {
                if (rows.Count >= 3)
                {
                    var target = Entities.UserDeviceTokens.OrderByDescending(p => p.Created)
                        .FirstOrDefault(p => p.UserNo == User.Identity.Name);
                    if (target != null)
                        target.DeviceToken = registration;
                    if (Entities.SaveChanges() > 0)
                        return new { ret_code = RetCode.Success, ret_msg = string.Empty };
                }
                else
                {
                    //如果推送表里没有该用户，应该插入一条该用户的数据，因为以后username都要从globaluserview获取，所以这个username字段的数据没有意义，将来应该删掉,默认语言设置为英语，客户端应该稍后调用get_application_type_list来更新语言
                    Entities.UserDeviceTokens.Add(new UserDeviceTokens
                    {
                        UserNo = User.Identity.Name,
                        LastDeviceLang = "en_US",
                        DeviceToken = registration,
                        Created = DateTime.Now,
                        StatusId = 1
                    });
                    if (Entities.SaveChanges() > 0)
                        return new { ret_code = RetCode.Success, ret_msg = string.Empty };
                }
            }
            else
            {
                return new { ret_code = RetCode.Success, ret_msg = "already exist in table" };
            }

            return new { ret_code = RetCode.Failure, ret_msg = "Upload failed" };
        }

        [Route("user_staff_info")]
        [HttpGet]
        public async Task<object> user_staff_info(string userno)
        {
            UserStaffInfo retValue = null;
            using (IUserManager userManager = Singleton<IUnityContainer>.Instance.Resolve<IUserManager>())
            {
                retValue = await Task.Factory.StartNew(() => userManager.SearchStaff(userno));
            }

            if (retValue != null)
                return new
                {
                    PositionName = retValue.PositionName,
                    StaffId = retValue.StaffId,
                    CellPhone = retValue.CellPhone,
                    Email = retValue.Email,
                    Company = retValue.Company,
                    DepartmentName = retValue.DepartmentName,
                    DepartmentTypeName = retValue.DepartmentTypeName,
                    Grade = retValue.Grade,
                    StaffName = retValue.StaffName,
                    RoleName = retValue.RoleName,
                    Ipt = retValue.Ipt,
                    Country = retValue.Country,
                    LeaveBalance = retValue.LeaveBalance
                };
            return null;
        }

        private object get_staffinfo_by_staffid(string staffId)
        {
            var info = Entities.StaffInfo.FirstOrDefault(si => si.StatusId > 0 && si.StaffId.Equals(staffId));
            if (info != null)
            {
                return new
                {
                    Id = info.StaffInfoId,
                    StaffId = info.StaffId,
                    EnglishFirstName = info.EnglishFirstName,
                    EnglishLastName = info.EnglishLastName,
                    LocalFirstName = info.LocalFirstName,
                    LocalLastName = info.LocalLastName,
                    ResidentialAddress = info.ResidentialAddress,
                    CorrespondenceAddress = info.CorrespondenceAddress,
                    HomeTel = info.HomeTelephone,
                    Mobile = info.MobileTelephone,
                    BankName = info.BankName,
                    BankAccount = info.BankAccountNumber,
                    MaritalStatus = info.MaritalStatus,
                    FamilyMembers = info.FamilyMembers,
                    Qualification = info.Qualification,
                    Email = info.EmailAddress,
                    EffectiveDate = info.EffectiveDate
                };
            }

            return new { ret_code = RetCode.Failure, ret_msg = "Can not find the staffinfo" };
        }

        //[Route("data/datachange_staffinfo")]
        //[HttpGet]
        //public object get_datachange_staffinfo(int flowCaseId)
        //{
        //    var flowcase = _entities.WF_FlowCases.FirstOrDefault(fc => fc.StatusId > 0 && fc.FlowCaseId == flowCaseId);
        //    if (flowcase != null)
        //    {
        //        return get_staffinfo_by_staffid(flowcase.UserNo);
        //    }
        //    return new { ret_code = RetCode.Failure, ret_msg = "Can not find the staffinfo" };
        //}
        [Route("datachange_staffinfo_bystaffid")]
        [HttpGet]
        public object get_datachange_staffinfo_by_staffid(string staffId)
        {
            return get_staffinfo_by_staffid(staffId);
        }

        [Route("get_leave_balance")]
        [HttpGet]
        public object get_leave_balance()
        {
            using (IUserManager userManager = Singleton<IUnityContainer>.Instance.Resolve<IUserManager>())
            {
                UserStaffInfo userInfo = userManager.SearchStaff(User.Identity.Name);
                ApplicationUser manager = new ApplicationUser(Entities, User.Identity.Name);
                double balance = userInfo?.LeaveBalance ?? 0;
                double notstarted = manager.GetNotStartedBalance(User.Identity.Name);
                double unapproved = manager.GetUnApprovedBalance(User.Identity.Name);
                double valid = balance > unapproved ? balance - unapproved : 0;
                return new { balance, notstarted, valid };
            }
        }

        [Route("get_mastercode_lastupdated")]
        [HttpGet]
        public long get_mastercode_lastupdated()
        {
            string value = Entities.Database
                .SqlQuery<string>(
                    "SELECT StringValue FROM dbo.AppSettings WHERE [Name] = 'MasterCodesLastUpdated' AND StatusId>0")
                .FirstOrDefault();
            long d;
            if (long.TryParse(value, out d))
                return d;
            return 0;
        }

        [Route("get_leave_type")]
        [HttpGet]
        public object get_leave_type(string lang = "ENG")
        {
            using (ApiClient client = new ApiClient())
            {
                var result = client.MasterCode_Search(new MasterCodeSearchParams
                {
                    country = Consts.GetApiCountry(),
                    id = "WFLVTY0",
                    language = lang,
                    code = "%"
                });
                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                {
                    var ret = result.ReturnValue.Select(r => new
                    {
                        code = r.ZZ03_CODE,
                        desc = r.ZZ03_CDC1
                    });
                    return ret.ToArray();
                }
                return new { ret_code = RetCode.Error, ret_msg = "ERROR FROM API:" + result.ErrorMessage };
            }
        }

        [Route("get_trip_destination")]
        [HttpGet]
        public object get_trip_destination(string lang = "ENG")
        {
            using (ApiClient client = new ApiClient())
            {
                var result = client.MasterCode_Search(new MasterCodeSearchParams
                {
                    country = Consts.GetApiCountry(),
                    id = "WFTRCT0",
                    language = lang,
                    code = "%"
                });
                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                {
                    var ret = result.ReturnValue.Select(r => new
                    {
                        code = r.ZZ03_CODE,
                        desc = r.ZZ03_CDC1
                    });
                    return ret.ToArray();
                }

                return new { ret_code = RetCode.Error, ret_msg = "ERROR FROM API:" + result.ErrorMessage };
            }
        }

        [AllowAnonymous]
        [Route("get_currency_type")]
        [HttpGet]
        public object get_currency_type(string lang = "ENG")
        {
            using (ApiClient client = new ApiClient())
            {
                var result = client.MasterCode_Search(new MasterCodeSearchParams
                {
                    country = Consts.GetApiCountry(),
                    id = "WFCURR0",
                    language = lang,
                    code = "%"
                });
                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                {
                    var ret = result.ReturnValue.Select(r => new
                    {
                        code = r.ZZ03_CODE,
                        desc = r.ZZ03_CDC1
                    });
                    return ret.ToArray();
                }

                return new { ret_code = RetCode.Error, ret_msg = "ERROR FROM API:" + result.ErrorMessage };
            }
        }

        [Route("get_expense_type")]
        [HttpGet]
        public object get_expense_type(string lang = "ENG")
        {
            using (ApiClient client = new ApiClient())
            {
                var result = client.MasterCode_Search(new MasterCodeSearchParams
                {
                    country = Consts.GetApiCountry(),
                    id = "WFEXPG0",
                    language = lang,
                    code = "%"
                });
                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                {
                    var ret = result.ReturnValue.Select(r => new
                    {
                        code = r.ZZ03_CODE,
                        desc = r.ZZ03_CDC1
                    });
                    return ret.ToArray();
                }

                return new { ret_code = RetCode.Error, ret_msg = "ERROR FROM API:" + result.ErrorMessage };
            }
        }

        [Route("get_staff_language")]
        [HttpGet]
        public object get_staff_language(string lang = "ENG")
        {
            using (ApiClient client = new ApiClient())
            {
                var result = client.MasterCode_Search(new MasterCodeSearchParams
                {
                    country = Consts.GetApiCountry(),
                    id = "WFLANG0",
                    language = lang,
                    code = "%"
                });
                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                {
                    var ret = result.ReturnValue.Select(r => new
                    {
                        code = r.ZZ03_CODE,
                        desc = r.ZZ03_CDC1
                    });
                    return ret.ToArray();
                }

                return new { ret_code = RetCode.Error, ret_msg = "ERROR FROM API:" + result.ErrorMessage };
            }
        }

        [Route("get_marriage_status")]
        [HttpGet]
        public object get_marriage_status(string lang = "ENG")
        {
            using (ApiClient client = new ApiClient())
            {
                var result = client.MasterCode_Search(new MasterCodeSearchParams
                {
                    country = Consts.GetApiCountry(),
                    id = "WFMRST0",
                    language = lang,
                    code = "%"
                });
                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                {
                    var ret = result.ReturnValue.Select(r => new
                    {
                        code = r.ZZ03_CODE,
                        desc = r.ZZ03_CDC1
                    });
                    return ret.ToArray();
                }

                return new { ret_code = RetCode.Error, ret_msg = "ERROR FROM API:" + result.ErrorMessage };
            }
        }

        [Route("get_country_region")]
        [HttpGet]
        public object get_country_region(string lang = "ENG")
        {
            using (ApiClient client = new ApiClient())
            {
                var result = client.MasterCode_Search(new MasterCodeSearchParams
                {
                    country = Consts.GetApiCountry(),
                    id = "WFCOUN0",
                    language = lang,
                    code = "%"
                });
                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                {
                    var ret = result.ReturnValue.Select(r => new
                    {
                        code = r.ZZ03_CODE,
                        desc = r.ZZ03_CDC1
                    });
                    return ret.ToArray();
                }

                return new { ret_code = RetCode.Error, ret_msg = "ERROR FROM API:" + result.ErrorMessage };
            }
        }

        [Route("get_All_Lang_WF_Type")]
        [HttpGet]
        [AllowAnonymous]
        public object get_All_Lang_WF_Type()
        {
            string[] langs = { "TWN", "CHN", "KOR", "ENG" };
            bool result = true;
            string error = "";
            List<GetApiModel> ret =new List<GetApiModel>();
            using (ApiClient client = new ApiClient())
            {
                foreach (string lang in langs)
                {
                    if (!result)
                    {
                        return null;
                    }
                    var leaveResult = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Consts.GetApiCountry(),
                        id = "WFLVTY0",
                        language = lang,
                        code = "%"
                    });
                    var tripResult = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Consts.GetApiCountry(),
                        id = "WFTRCT0",
                        language = lang,
                        code = "%"
                    });
                    var currencyResult = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Consts.GetApiCountry(),
                        id = "WFCURR0",
                        language = lang,
                        code = "%"
                    });
                    var expenseResult = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Consts.GetApiCountry(),
                        id = "WFEXPG0",
                        language = lang,
                        code = "%"
                    });
                    var staffResult = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Consts.GetApiCountry(),
                        id = "WFLANG0",
                        language = lang,
                        code = "%"
                    });
                    var marriageResult = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Consts.GetApiCountry(),
                        id = "WFMRST0",
                        language = lang,
                        code = "%"
                    });
                    var countryResult = client.MasterCode_Search(new MasterCodeSearchParams
                    {
                        country = Consts.GetApiCountry(),
                        id = "WFCOUN0",
                        language = lang,
                        code = "%"
                    });
                    if (string.IsNullOrWhiteSpace(leaveResult.ErrorMessage) && leaveResult.ReturnValue != null &&
                        string.IsNullOrWhiteSpace(tripResult.ErrorMessage) && tripResult.ReturnValue != null &&
                        string.IsNullOrWhiteSpace(currencyResult.ErrorMessage) && currencyResult.ReturnValue != null &&
                        string.IsNullOrWhiteSpace(expenseResult.ErrorMessage) && expenseResult.ReturnValue != null &&
                        string.IsNullOrWhiteSpace(staffResult.ErrorMessage) && staffResult.ReturnValue != null &&
                        string.IsNullOrWhiteSpace(marriageResult.ErrorMessage) && marriageResult.ReturnValue != null &&
                        string.IsNullOrWhiteSpace(countryResult.ErrorMessage) && countryResult.ReturnValue != null)
                    {
                        var results = leaveResult.ReturnValue.Select(p => new GetApiModel
                        {
                            code = p.ZZ03_CODE,
                            desc = p.ZZ03_CDC1,
                            type = "LeaveType",
                            lang = lang
                        }).Concat(tripResult.ReturnValue.Select(p => new GetApiModel
                        {
                            code = p.ZZ03_CODE,
                            desc = p.ZZ03_CDC1,
                            type = "TripDestination",
                            lang = lang
                        })).Concat(currencyResult.ReturnValue.Select(p => new GetApiModel
                        {
                            code = p.ZZ03_CODE,
                            desc = p.ZZ03_CDC1,
                            type = "CurrencyType",
                            lang = lang
                        })).Concat(expenseResult.ReturnValue.Select(p => new GetApiModel
                        {
                            code = p.ZZ03_CODE,
                            desc = p.ZZ03_CDC1,
                            type = "ExpenseType",
                            lang = lang
                        })).Concat(staffResult.ReturnValue.Select(p => new GetApiModel
                        {
                            code = p.ZZ03_CODE,
                            desc = p.ZZ03_CDC1,
                            type = "StaffLanguage",
                            lang = lang
                        })).Concat(marriageResult.ReturnValue.Select(p => new GetApiModel
                        {
                            code = p.ZZ03_CODE,
                            desc = p.ZZ03_CDC1,
                            type = "MarriageStatus",
                            lang = lang
                        })).Concat(countryResult.ReturnValue.Select(p => new GetApiModel
                        {
                            code = p.ZZ03_CODE,
                            desc = p.ZZ03_CDC1,
                            type = "CountryRegion",
                            lang = lang
                        }));  ;
                        ret = results.ToList().Concat(ret).ToList();
                    }
                    else
                    {
                        result = false;
                        if (!string.IsNullOrWhiteSpace(leaveResult.ErrorMessage))
                            error += "ERROR FROM API:LeaveType" + lang + leaveResult.ErrorMessage;
                        if (!string.IsNullOrWhiteSpace(tripResult.ErrorMessage))
                            error += "ERROR FROM API:TripDestination" + lang + tripResult.ErrorMessage;
                        if (!string.IsNullOrWhiteSpace(currencyResult.ErrorMessage))
                            error += "ERROR FROM API:CurrencyType" + lang + currencyResult.ErrorMessage;
                        if (!string.IsNullOrWhiteSpace(expenseResult.ErrorMessage))
                            error += "ERROR FROM API:ExpenseType" + lang + expenseResult.ErrorMessage;
                        if (!string.IsNullOrWhiteSpace(staffResult.ErrorMessage))
                            error += "ERROR FROM API:StaffLanguage" + lang + staffResult.ErrorMessage;
                        if (!string.IsNullOrWhiteSpace(marriageResult.ErrorMessage))
                            error += "ERROR FROM API:MarriageStatus" + lang + marriageResult.ErrorMessage;
                        if (!string.IsNullOrWhiteSpace(countryResult.ErrorMessage))
                            error += "ERROR FROM API:CountryRegion" + lang + countryResult.ErrorMessage;
                    }
                }
            }
            if (result)
            {
                return ret.ToArray();
            }
            else
            {
                return new { ret_code = RetCode.Error, ret_msg = error };
            }
        }

        [AllowAnonymous]
        [Route("get_template_icon")]
        [HttpGet]
        public HttpResponseMessage get_template_icon(string path)
        {
            if (!File.Exists(path))
                return new HttpResponseMessage(HttpStatusCode.NoContent) { ReasonPhrase = "File not existed." };
            string contentType = GetContentType(path);
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return response;
        }
    }

    public class PropertiesModel
    {
        public string[] properties { get; set; }
        public int? flowCaseId { get; set; }
    }

    public class GetApiModel
    {
        public string lang { get; set; }
        public string code { get; set; }
        public string desc { get; set; }
        public string type { get; set; }

    }
}