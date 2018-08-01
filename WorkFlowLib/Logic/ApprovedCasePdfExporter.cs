using iTextSharp.text;
using iTextSharp.text.pdf;
using Dreamlab.Core;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using WorkFlowLib.DTO;
using WorkFlowLib.Data;
using WorkFlowLib.Parameters;

namespace WorkFlowLib.Logic
{
    public class ApprovedCasePdfExporter
    {
        #region Properties
        private static Dictionary<string, string> BrandImages
        {
            get
            {
                return new Dictionary<string, string>
                {
                    { "HCT", "Brands_HCT"},
                    { "HTN", "Brands_HT"},
                    { "ROS", "Brands_RTS"},
                    { "APM", "Brands_AP"},
                    { "LEO", "Brands_LEO"}
                };
            }
        }
        private static Font FontNormal
        {
            get
            {
                return FontFactory.GetFont("Arial", 10, Font.NORMAL, BaseColor.BLACK);
            }
        }
        private static BaseColor BackgroundColor
        {
            get
            {
                return new BaseColor(230, 230, 230);
            }
        }
        private static Font FontBold
        {
            get
            {
                return FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.BLACK);
            }

        }
        #endregion

        public static byte[] GenerateFlowCase(PDFFlowCase model, string userNo)
        {
            iTextSharp.text.Rectangle pageSize = new iTextSharp.text.Rectangle(PageSize.A4.Width, PageSize.A4.Height);

            pageSize.BackgroundColor = BackgroundColor;
            Document document = new Document(pageSize, 0, 0, 10, 10);
            using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
            {
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                DrawFlowSteps(model.FlowInfo, document);

                document.Add(new Paragraph(" "));
                document.Add(DrawLine());

                document.Add(DrawTitle(model.FlowInfo, userNo));

                #region Workflow
                if (model.FlowType != null && model.FlowType.TemplateType == 1)
                    DrawViewStoreApprovalForm(model, document);
                else if (model.FlowType != null && model.FlowType.TemplateType == 2)
                    DrawViewLeaveApplication(model, document);
                else if (model.FlowType != null && model.FlowType.TemplateType == 7)
                    DrawViewStoreClosureForm(model, document);
                else
                    DrawViewProperties(model, document);
                #endregion

                #region Logs
                if (model.CaseLogs != null && model.CaseLogs.Length > 0)
                {
                    document.Add(new Paragraph(" "));
                    document.Add(DrawCaseLogs(model.CaseLogs));
                }
                #endregion

                document.Close();
                return memoryStream.ToArray();
            }
        }

        #region Draw View
        private static void DrawFlowSteps(FlowInfo flowInfo, Document document)
        {
            var groups = flowInfo.StepGroups.OrderBy(p => p.OrderId).ToArray();
            PdfPTable table = new PdfPTable(2 + (groups.Length * 2 - 1));
            table.PaddingTop = 15;
            Phrase phrase = null;
            PdfPCell cell = null;
            List<PdfPCell> cells = new List<PdfPCell>();
            Font fontSmall = FontFactory.GetFont("Arial", 8, Font.NORMAL, BaseColor.BLACK);
            Font font = FontFactory.GetFont("Arial", 10, Font.NORMAL, BaseColor.WHITE);
            Phrase phraseBreakLine = new Phrase("\n");

            phrase = new Phrase();
            phrase.Add(new Paragraph("Applicant", fontSmall));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
            cell.VerticalAlignment = PdfPCell.ALIGN_MIDDLE;

            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BackgroundColor;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);

            cell = new PdfPCell();
            cell.BackgroundColor = BackgroundColor;
            cell.BorderColor = BackgroundColor;
            cell.Colspan = (groups.Length * 2 - 1) + 1;
            table.AddCell(cell);
            document.Add(table);

            var flowCase = flowInfo.CaseInfo;
            phrase = new Phrase();
            phrase.Add(phraseBreakLine);
            phrase.Add(new Paragraph($"{flowCase.Department}", font));
            phrase.Add(phraseBreakLine);
            phrase.Add(new Paragraph(" "));
            phrase.Add(new Paragraph($"{GetWF_UsernameByNo(flowCase.Applicant)}", font));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
            cell.VerticalAlignment = PdfPCell.ALIGN_MIDDLE;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BackgroundColor;
            cell.BackgroundColor = new BaseColor(115, 185, 65);

            cells.Add(cell);

            phrase = new Phrase();
            cell = ImageCell("Approved Green Arrow", 25f, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_MIDDLE;
            cell.BorderColor = BackgroundColor;
            cells.Add(cell);

            for (int i = 0; i < groups.Length; i++)
            {
                var stepGroup = flowInfo.StepGroups.OrderBy(p => p.OrderId).ToArray()[i];
                var isActive = (groups[i].StepStatus == StepStatus.Approved);
                int childStepCount = 0;

                phrase = new Phrase();
                foreach (var step in stepGroup.Steps.OrderBy(p => p.OrderId))
                {
                    childStepCount++;
                    var status = flowCase.StepResults.FirstOrDefault(p => p.FlowStepId == step.FlowStepId)?.Status;
                    var department = "";
                    var usernameByNo = "";

                    if (step.NoApprover.GetValueOrDefault())
                    {
                        phrase.Add(new Paragraph("No Approver", font));
                        phrase.Add(phraseBreakLine);
                    }

                    if (step.FinalApprover != null)
                    {
                        department = step.FinalDepartment;
                        usernameByNo = GetWF_UsernameByNo(step.FinalApprover);
                    }
                    else if (step.ApproverType == (int)ApproverType.Person)
                    {
                        department = step.Department;
                        usernameByNo = GetWF_UsernameByNo(step.Approver);
                    }
                    else
                    {
                        department = step.GetApprover();
                        usernameByNo = GetWF_UsernameByNo(step.FinalApprover);
                    }

                    phrase.Add(new Paragraph($"{department}", font));
                    phrase.Add(phraseBreakLine);
                    phrase.Add(new Paragraph(" "));
                    phrase.Add(new Paragraph($"{usernameByNo}", font));
                    phrase.Add(phraseBreakLine);
                    if (childStepCount > 1)
                    {
                        if (stepGroup.StepConditionId == 1)
                            phrase.Add(new Paragraph("All", font));
                        if (stepGroup.StepConditionId == 2)
                            phrase.Add(new Paragraph("Any", font));
                    }
                }
                cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
                cell.VerticalAlignment = PdfPCell.ALIGN_MIDDLE;
                cell.BackgroundColor = new BaseColor(115, 185, 65);
                cell.PaddingLeft = 5f;
                cell.PaddingTop = 5f;
                cell.PaddingRight = 5f;
                cell.PaddingBottom = 5f;
                cell.BorderColor = BackgroundColor;
                cells.Add(cell);

                if (i < groups.Length - 1)
                {
                    cell = ImageCell("Approved Green Arrow", 25f, PdfPCell.ALIGN_CENTER);
                    cell.VerticalAlignment = PdfPCell.ALIGN_MIDDLE;
                    cell.BorderColor = BackgroundColor;
                    cells.Add(cell);
                }
                if (i < groups.Length - 1 && stepGroup.NotificationUsers.Any())
                {
                    phrase = new Phrase();
                    foreach (var item in stepGroup.NotificationUsers)
                        phrase.Add(new Paragraph($"{GetWF_UsernameByNo(item.GetNotifyUser())}", font));

                    cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.BorderColor = BackgroundColor;
                    cells.Add(cell);
                }
            }

            PdfPRow row = new PdfPRow(cells.ToArray());
            table = new PdfPTable(cells.Count());
            table.Rows.Add(row);
            document.Add(table);
        }

        private static PdfPTable DrawTitle(FlowInfo flowInfo, string userNo)
        {
            PdfPTable table = new PdfPTable(1);
            Font font = FontFactory.GetFont("Arial", 16, Font.NORMAL, BaseColor.RED);
            Phrase phrase = new Phrase();
            PdfPCell cell = null;

            if (flowInfo.CaseInfo.Applicant.EqualsIgnoreCaseAndBlank(userNo))
                phrase.Add(new Chunk($"Application \"{flowInfo.CaseInfo.Subject}\" All Approvals Obtained ", font));
            else
                phrase.Add(new Chunk($"Application \"{flowInfo.CaseInfo.Subject}\" Fully Approved", font));

            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_MIDDLE;
            cell.PaddingTop = 15f;
            cell.PaddingBottom = 15f;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);
            return table;
        }

        #region ViewStoreApproval
        private static void DrawViewStoreApprovalForm(PDFFlowCase model, Document document)
        {
            PdfPTable table = new PdfPTable(2);
            PdfPTable childTable = null;
            Phrase phrase = null;
            PdfPCell cell = null;

            Font fontTitle = FontFactory.GetFont("Arial", 13, Font.NORMAL, BaseColor.BLUE);
            Font fontTitlePanel = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);
            Font small = FontFactory.GetFont("Arial", 6, Font.NORMAL, BaseColor.BLACK);
            BaseColor headerColor = new BaseColor(127, 127, 127);

            BaseColor color = new BaseColor(51, 122, 183);
            Phrase phraseBreakLine = new Phrase("\n");

            #region Header
            // Version
            phrase = new Phrase();
            phrase.Add(new Chunk($"Version:  { model.FlowInfo.CaseInfo.Ver ?? 0}", fontTitle));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingBottom = 10f;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Paragraph("Brand", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);

            var checkedBrand = GetPropertyValueByPropertyName(model.PropertiesValue, "Brand");
            if (!string.IsNullOrEmpty(checkedBrand))
            {
                if (BrandImages.ContainsKey(checkedBrand))
                    cell = ImageCell(BrandImages[checkedBrand], 25f, PdfPCell.ALIGN_CENTER);
                else
                    cell = new PdfPCell();
                cell.PaddingTop = 5f;
                cell.PaddingBottom = 5f;
                cell.BorderColor = BackgroundColor;
                table.AddCell(cell);
            }
            else
            {
                cell = new PdfPCell();
                cell.BorderColor = BackgroundColor;
                table.AddCell(cell);
            }
            document.Add(table);
            #endregion

            document.Add(new Paragraph(" "));

            #region Reason for application
            table = new PdfPTable(2);
            phrase = new Phrase();
            phrase.Add(new Chunk("Reason for application", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 10f;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = color;
            table.AddCell(cell);

            phrase = new Phrase();
            string reason = GetPropertyValueByPropertyName(model.PropertiesValue, "ReasonForApplication");
            phrase.Add(new Chunk($"{reason}  ", FontBold));
            if (reason == "True")
                phrase.Add(new Chunk("Replacement Store  ", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 10f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk($"Deadline:  {model.FlowInfo.CaseInfo.Deadline?.ToLocalTime().ToString("M/d/yyyy") ?? string.Empty}", FontBold));
            phrase.Add(new Chunk(model.FlowInfo.CaseInfo.Deadline?.ToLocalTime().ToString("M/d/yyyy") ?? string.Empty, FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 10f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            document.Add(table);
            #endregion

            document.Add(new Paragraph(" "));

            #region Store Informatio
            table = new PdfPTable(2);
            phrase = new Phrase();
            phrase.Add(new Chunk("Store Information", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingLeft = 5;
            cell.PaddingTop = 10f;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = color;
            table.AddCell(cell);
            //Store Type
            phrase = new Phrase();
            phrase.Add(new Chunk("Store Type:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreType"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Store Name
            phrase = new Phrase();
            phrase.Add(new Chunk($"Store Name:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreNamePrefix"), FontNormal));
            phrase.Add(phraseBreakLine);
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreNamePrefix"), FontNormal));
            phrase.Add(phraseBreakLine);
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "City"), FontNormal));
            phrase.Add(phraseBreakLine);
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreLocation"), FontNormal));
            phrase.Add(phraseBreakLine);
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreTypeForName"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Shop Code
            phrase = new Phrase();
            phrase.Add(new Chunk("Shop Code:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ShopCode"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //City Tier
            phrase = new Phrase();
            phrase.Add(new Chunk("City Tier:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "CityTier"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //City Tier
            phrase = new Phrase();
            phrase.Add(new Chunk("Mall/Dept Store Tier:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "MallDeptStoreTier"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Store Size (sq.m)
            phrase = new Phrase();
            phrase.Add(new Chunk("Store Size (sq.m):  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreSize"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //# of Staff
            phrase = new Phrase();
            phrase.Add(new Chunk("# of Staff:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "OfSalesStaff"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Expected Opening Date
            phrase = new Phrase();
            phrase.Add(new Chunk("Expected Opening Date:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ExpectedOpeningDate"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Location Customer Profile
            phrase = new Phrase();
            phrase.Add(new Chunk("Location Customer Profile:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "LocationCustomerProfile"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Location Traffic
            phrase = new Phrase();
            phrase.Add(new Chunk("Location Traffic:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "LocationTraffic"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            document.Add(table);
            #endregion

            document.Add(new Paragraph(" "));

            #region Key Contract Terms
            table = new PdfPTable(3);
            phrase = new Phrase();
            phrase.Add(new Chunk("Key Contract Terms", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 3;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 10f;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = color;
            table.AddCell(cell);

            //Name of Landlord/Operator
            phrase = new Phrase();
            phrase.Add(new Chunk("Name of Landlord/Operator:  ", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 15f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthBottom = 1f;
            cell.BorderColorBottom = BaseColor.BLACK;
            table.AddCell(cell);

            // NameOfLandlord
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "NameOfLandlord"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 15f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthBottom = 1f;
            cell.BorderColorBottom = BaseColor.BLACK;
            table.AddCell(cell);
            //Credit Terms(if applicable):
            if (hideControl(model.PropertiesValue, "CreditTerms"))
            {
                phrase = new Phrase();
                phrase.Add(new Chunk("Credit Terms(if applicable):  ", FontBold));
                phrase.Add(new Chunk((GetPropertyValueByPropertyName(model.PropertiesValue, "CreditTerms") + " days"), FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.PaddingTop = 5f;
                cell.PaddingLeft = 5f;
                cell.PaddingBottom = 15f;
                cell.PaddingRight = 5f;
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderWidthBottom = 1f;
                cell.BorderColorBottom = BaseColor.BLACK;
                table.AddCell(cell);
            }
            else
            {
                phrase = new Phrase();
                phrase.Add(new Chunk(" ", FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.PaddingTop = 5f;
                cell.PaddingLeft = 5f;
                cell.PaddingBottom = 15f;
                cell.PaddingRight = 5f;
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderWidthBottom = 1f;
                cell.BorderColorBottom = BaseColor.BLACK;
                table.AddCell(cell);
            }

            //Contract Period
            phrase = new Phrase();
            phrase.Add(new Chunk("Contract Period:  ", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 15f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Contract Period
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ContractPeriodFrom"), FontNormal));
            phrase.Add(new Chunk("   to   ", FontNormal));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ContractPeriodTo"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 15f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            phrase = new Phrase();
            Chunk st = new Chunk("st", small);
            st.SetTextRise(7);
            phrase.Add(new Chunk("1", FontBold));
            phrase.Add(st);
            phrase.Add(new Chunk(" Renewal Period:  ", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Renewal Period
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "FirstRenewalPeriodFrom"), FontNormal));
            phrase.Add(new Chunk("   to  ", FontNormal));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "FirstRenewalPeriodTo"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            // 2<sup>st</sup>Renewal Period:
            phrase = new Phrase();
            phrase.Add(new Chunk("2", FontBold));
            phrase.Add(st);
            phrase.Add(new Chunk(" Renewal Period:  ", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 15f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColorBottom = BaseColor.BLACK;
            cell.BorderWidthBottom = 1f;
            table.AddCell(cell);

            //Renewal Period
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "SecondRenewalPeriodFrom"), FontNormal));
            phrase.Add(new Chunk("   to  ", FontNormal));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "SecondRenewalPeriodTo"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 15f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColorBottom = BaseColor.BLACK;
            cell.BorderWidthBottom = 1f;
            table.AddCell(cell);

            #region Chirlden Rental/Lease Amount            
            childTable = new PdfPTable(3);
            var CurrentTermType = GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermType");
            var PreviousTermType = GetPropertyValueByPropertyName(model.PropertiesValue, "PreviousTermType");
            var rentFlag = CurrentTermType.EqualsIgnoreCaseAndBlank("Fixed Rent");

            phrase = new Phrase();
            phrase.Add(new Chunk(" ", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);
            //Current Term
            phrase = new Phrase();
            phrase.Add(new Chunk("Current Term", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);
            //Previous Term
            phrase = new Phrase();
            phrase.Add(new Chunk("Previous Term", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Fixed Rent ", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);
            //Rent
            phrase = new Phrase();
            phrase.Add(new Chunk("Rent", FontNormal));
            phrase.Add(new Chunk((rentFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermRent") : ""), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);
            //Rent
            phrase = new Phrase();
            rentFlag = PreviousTermType.EqualsIgnoreCaseAndBlank("Fixed Rent");
            phrase.Add(new Chunk("Rent", FontNormal));
            phrase.Add(new Chunk(((rentFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "PreviousTermRent") : "")), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);

            //Turnover % with Minimum Fixed Rent
            phrase = new Phrase();
            phrase.Add(new Chunk("Turnover % with Minimum Fixed Rent ", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);

            var turnoverFlag = CurrentTermType.EqualsIgnoreCaseAndBlank("Turnover % with Minimum Fixed Rent");
            phrase = new Phrase();
            phrase.Add(new Chunk("Turnover %  ", FontNormal));
            phrase.Add(new Chunk((turnoverFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermTurnover") : ""), FontNormal));
            phrase.Add(phraseBreakLine);
            phrase.Add(new Chunk("Rent  ", FontNormal));

            phrase.Add(new Chunk((turnoverFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermRent") : ""), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);

            turnoverFlag = PreviousTermType.EqualsIgnoreCaseAndBlank("Turnover % with Minimum Fixed Rent");
            phrase = new Phrase();
            phrase.Add(new Chunk("Turnover %   ", FontNormal));
            phrase.Add(new Chunk((turnoverFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "PreviousTermTurnover") : ""), FontNormal));
            phrase.Add(phraseBreakLine);
            phrase.Add(new Chunk("Rent   ", FontNormal));
            phrase.Add(new Chunk((turnoverFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "PreviousTermRent") : ""), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);

            //Fixed Rent + Turnover %
            phrase = new Phrase();
            phrase.Add(new Chunk("Fixed Rent + Turnover %", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);

            var FixedRentFlag = CurrentTermType.EqualsIgnoreCaseAndBlank("Fixed Rent + Turnover %");
            phrase = new Phrase();
            phrase.Add(new Chunk("Turnover %   ", FontNormal));
            phrase.Add(new Chunk((FixedRentFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermTurnover") : ""), FontNormal));
            phrase.Add(phraseBreakLine);
            phrase.Add(new Chunk("Rent   ", FontNormal));
            phrase.Add(new Chunk((FixedRentFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermRent") : ""), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);

            FixedRentFlag = PreviousTermType.EqualsIgnoreCaseAndBlank("Fixed Rent + Turnover %");
            phrase = new Phrase();
            phrase.Add(new Chunk("Turnover %   ", FontNormal));
            phrase.Add(new Chunk((FixedRentFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "PreviousTermTurnover") : ""), FontNormal));
            phrase.Add(phraseBreakLine);
            phrase.Add(new Chunk("Rent   ", FontNormal));
            phrase.Add(new Chunk((FixedRentFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "PreviousTermRent") : ""), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.GRAY;
            cell.BorderWidth = 1;
            childTable.AddCell(cell);

            cell = new PdfPCell(childTable);
            //cell.AddElement(childTable);
            cell.Colspan = 3;
            cell.Padding = 10;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            table.AddCell(cell);
            #endregion

            document.Add(table);

            table = new PdfPTable(2);
            table.PaddingTop = 10;

            //Deposit/Key Money:
            phrase = new Phrase();
            phrase.Add(new Chunk("Deposit/Key Money: ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "CashAmount"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthTop = 1;
            cell.BorderColorTop = BaseColor.BLACK;

            table.AddCell(cell);
            //Refundable
            phrase = new Phrase();
            phrase.Add(new Chunk("Refundable? ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "MoneyRefundable"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthTop = 1;
            cell.BorderColorTop = BaseColor.BLACK;

            table.AddCell(cell);

            //Premium(if applicable):
            if (hideControl(model.PropertiesValue, "Premium"))
            {
                phrase = new Phrase();
                phrase.Add(new Chunk("Premium(if applicable):  ", FontBold));
                phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "Premium"), FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.Colspan = 2;
                cell.PaddingTop = 10f;
                cell.PaddingLeft = 5;
                cell.PaddingBottom = 10f;
                cell.BackgroundColor = BaseColor.WHITE;
                table.AddCell(cell);
            }

            document.Add(table);
            #endregion

            document.Add(new Paragraph(" "));

            DrawFinancialInformation(model, document);

            var StoreType = GetPropertyValueByPropertyName(model.PropertiesValue, "StoreType");
            if (StoreType.EqualsIgnoreCaseAndBlank("Franchise"))
            {
                document.Add(new Paragraph(" "));
                //P&L Summary
                table = new PdfPTable(2);
                phrase = new Phrase();
                phrase.Add(new Chunk("Franchisee Information", fontTitlePanel));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.Colspan = 2;
                cell.PaddingTop = 5f;
                cell.PaddingBottom = 5f;
                cell.BackgroundColor = headerColor;
                table.AddCell(cell);

                //Construction Cost Breakdown
                if (hideControls(model.PropertiesValue, "DeptStoreLeaseContract,Rent,InventoryRisk,ConstructionWallsCeilingFlooring,LooseFumiture,Staff,StoreOpsExpenses,CollectMoneyFromDeptStorePOS,POSSystem,PaidByFranchiseeToBLS"))
                {
                    document.Add(new Paragraph(" "));
                    var POSSales = GetPropertyValueByPropertyName(model.PropertiesValue, "POSSales");
                    var Rental = GetPropertyValueByPropertyName(model.PropertiesValue, "Rental");
                    var ChargedByLandlord = GetPropertyValueByPropertyName(model.PropertiesValue, "ChargedByLandlord");
                    var ChargedToFranchisee = GetPropertyValueByPropertyName(model.PropertiesValue, "ChargedToFranchisee");
                    var FeePercentage = GetPropertyValueByPropertyName(model.PropertiesValue, "FeePercentage");
                    var ManagementServiceFee = string.Empty;
                    if (POSSales != null && Rental != null && ChargedByLandlord != null && ChargedToFranchisee != null && FeePercentage != null)
                    {
                        var fee = double.Parse(POSSales) * double.Parse(FeePercentage) / 100 - double.Parse(Rental) - double.Parse(ChargedByLandlord) - double.Parse(ChargedToFranchisee);
                        ManagementServiceFee = fee.ToString("#.##");
                    }

                    childTable = new PdfPTable(3);
                    //Name of Franchisee:
                    if (hideControl(model.PropertiesValue, "NameOfFranchisee"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Name of Franchisee:  ", FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "NameOfFranchisee"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.Colspan = 2;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }
                    //Dept Store/Lease Contract:
                    if (hideControl(model.PropertiesValue, "DeptStoreLeaseContract"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Dept Store/Lease Contract:  ", FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "DeptStoreLeaseContract"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "DeptStoreLeaseContractRemark"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }
                    //Rent:
                    if (hideControl(model.PropertiesValue, "Rent"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Rent:  ", FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "Rent"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "RentRemark"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //InventoryRisk
                    if (hideControl(model.PropertiesValue, "InventoryRisk"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Inventory Risk:  ", FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "InventoryRisk"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "InventoryRiskRemark"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //Construction-Walls, Celling, Flooring:
                    if (hideControl(model.PropertiesValue, "ConstructionWallsCeilingFlooring"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Construction-Walls, Celling, Flooring:  ", FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ConstructionWallsCeilingFlooring"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ConstructionWallsCeilingFlooringRemark"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //Construction-Walls, Celling, Flooring:
                    if (hideControl(model.PropertiesValue, "LooseFumiture"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Loose Fumiture:  ", FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "LooseFumiture"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "LooseFumitureRemark"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //Staff
                    if (hideControl(model.PropertiesValue, "Staff"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Staff:  ", FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "Staff"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StaffRemark"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //Store Ops Expenses
                    if (hideControl(model.PropertiesValue, "StoreOpsExpenses"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Store Ops Expenses:  ", FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreOpsExpenses"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreOpsExpensesRemark"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //Collect money from Dept. Store/POS
                    if (hideControl(model.PropertiesValue, "CollectMoneyFromDeptStorePOS"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Collect money from Dept. Store/POS:  ", FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "CollectMoneyFromDeptStorePOS"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "CollectMoneyFromDeptStorePOSRemark"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //POS System:
                    if (hideControl(model.PropertiesValue, "POSSystem"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("POS System:  ", FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "POSSystem"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "POSSystemRemark"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //Deposit to be paid by Franchisee to BLS:
                    if (hideControl(model.PropertiesValue, "PaidByFranchiseeToBLS"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Deposit to be paid by Franchisee to BLS:  ", FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 10f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        cell.BorderColorBottom = BaseColor.GRAY;
                        cell.BorderWidthBottom = 1;
                        childTable.AddCell(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "PaidByFranchiseeToBLS"), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.Colspan = 2;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 10f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        cell.BorderColorBottom = BaseColor.GRAY;
                        cell.BorderWidthBottom = 1;
                        childTable.AddCell(cell);
                    }
                    childTable = new PdfPTable(3);
                    cell = new PdfPCell(childTable);
                    cell.Padding = 0;
                    cell.BackgroundColor = BaseColor.WHITE; ;
                    cell.Colspan = 2;
                    table.AddCell(cell);
                }

                if (hideControls(model.PropertiesValue, "POSSales,Rental,ChargedByLandlord,ChargedToFranchisee,FeePercentage"))
                {
                    document.Add(new Paragraph(" "));

                    var POSSales = GetPropertyValueByPropertyName(model.PropertiesValue, "POSSales");
                    var Rental = GetPropertyValueByPropertyName(model.PropertiesValue, "Rental");
                    var ChargedByLandlord = GetPropertyValueByPropertyName(model.PropertiesValue, "ChargedByLandlord");
                    var ChargedToFranchisee = GetPropertyValueByPropertyName(model.PropertiesValue, "ChargedToFranchisee");
                    var FeePercentage = GetPropertyValueByPropertyName(model.PropertiesValue, "FeePercentage");
                    var ManagementServiceFee = string.Empty;
                    if (POSSales != null && Rental != null && ChargedByLandlord != null && ChargedToFranchisee != null && FeePercentage != null)
                    {
                        var fee = double.Parse(POSSales) * double.Parse(FeePercentage) / 100 - double.Parse(Rental) - double.Parse(ChargedByLandlord) - double.Parse(ChargedToFranchisee);
                        ManagementServiceFee = fee.ToString("#.##");
                    }

                    #region Left table
                    childTable = new PdfPTable(1);
                    //POS Sales:
                    if (hideControl(model.PropertiesValue, "POSSales"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("POS Sales:  ", FontBold));
                        phrase.Add(new Chunk(POSSales, FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }
                    //Commission %:
                    if (hideControl(model.PropertiesValue, "FeePercentage"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Commission %:  ", FontBold));
                        phrase.Add(new Chunk((FeePercentage != null ? (FeePercentage + " %") : ""), FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //Rental %:
                    if (hideControl(model.PropertiesValue, "Rental"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Rental:  ", FontBold));
                        phrase.Add(new Chunk(Rental, FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //ChargedByLandlord
                    if (hideControl(model.PropertiesValue, "ChargedByLandlord"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Misc. Exp. Charged by Landlord:  ", FontBold));
                        phrase.Add(new Chunk(ChargedByLandlord, FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //ChargedToFranchisee
                    if (hideControl(model.PropertiesValue, "ChargedToFranchisee"))
                    {
                        phrase = new Phrase();
                        phrase.Add(new Chunk("Misc. Exp. Charged to Franchisee by BLS:  ", FontBold));
                        phrase.Add(new Chunk(ChargedToFranchisee, FontNormal));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingBottom = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                        childTable.AddCell(cell);
                    }

                    //Management Service Fee Calculation
                    phrase = new Phrase();
                    phrase.Add(new Chunk("Management Service Fee Calculation:  ", FontBold));
                    phrase.Add(new Chunk(ManagementServiceFee, FontNormal));
                    cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.PaddingTop = 5f;
                    cell.PaddingBottom = 5f;
                    cell.BackgroundColor = BaseColor.WHITE;
                    childTable.AddCell(cell);

                    cell = new PdfPCell(childTable);
                    cell.Padding = 0;
                    cell.BackgroundColor = BaseColor.WHITE; ;
                    table.AddCell(cell);
                    #endregion

                    #region Right table
                    childTable = new PdfPTable(1);
                    //Definitions:
                    phrase = new Phrase();
                    phrase.Add(new Chunk("Definitions:  ", FontNormal));
                    cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.PaddingTop = 5f;
                    cell.PaddingBottom = 5f;
                    cell.BackgroundColor = BaseColor.WHITE;
                    childTable.AddCell(cell);
                    //POS Sales (P) = Sales from customers, ex-VAT

                    phrase = new Phrase();
                    phrase.Add(new Chunk("POS Sales (P) = Sales from customers, ex-VAT", FontBold));
                    cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.PaddingTop = 5f;
                    cell.PaddingBottom = 5f;
                    cell.BackgroundColor = BaseColor.WHITE;
                    childTable.AddCell(cell);

                    //Rental (Y) = Rental / Landlord Commission, ex-VAT, less promo activities discount or rebate, other non-invoiced expenses

                    phrase = new Phrase();
                    phrase.Add(new Chunk("Rental (Y) = Rental / Landlord Commission, ex-VAT, less promo activities discount or rebate, other non-invoiced expenses ", FontBold));
                    cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.PaddingTop = 5f;
                    cell.PaddingBottom = 5f;
                    cell.BackgroundColor = BaseColor.WHITE;
                    childTable.AddCell(cell);

                    //Mise. Exp. Charged by Landlord (A) = Invoiced expenses, ex-VAT, as charged or reimbursed by landlord

                    phrase = new Phrase();
                    phrase.Add(new Chunk("Mise. Exp. Charged by Landlord (A) = Invoiced expenses, ex-VAT, as charged or reimbursed by landlord  ", FontBold));
                    cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.PaddingTop = 5f;
                    cell.PaddingBottom = 5f;
                    cell.BackgroundColor = BaseColor.WHITE;
                    childTable.AddCell(cell);


                    //Misc. Exp. Charged to Franchisee by BLS (B) = damages and expense, ex-VAT, chargeabled by BLS

                    phrase = new Phrase();
                    phrase.Add(new Chunk("Misc. Exp. Charged to Franchisee by BLS (B) = damages and expense, ex-VAT, chargeabled by BLS  ", FontBold));
                    cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.PaddingTop = 5f;
                    cell.PaddingBottom = 5f;
                    cell.BackgroundColor = BaseColor.WHITE;
                    childTable.AddCell(cell);

                    cell = new PdfPCell(childTable);
                    cell.Padding = 0;
                    cell.BackgroundColor = BaseColor.WHITE; ;
                    table.AddCell(cell);
                    #endregion
                }

                if (hideControl(model.PropertiesValue, "OtherInformation"))
                {
                    document.Add(new Paragraph(" "));

                    phrase = new Phrase();
                    phrase.Add(new Chunk("Other Information", fontTitlePanel));
                    cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.Colspan = 2;
                    cell.PaddingTop = 5f;
                    cell.PaddingBottom = 5f;
                    cell.BackgroundColor = BaseColor.WHITE;
                    table.AddCell(cell);

                    phrase = new Phrase();
                    phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "OtherInformation"), fontTitlePanel));
                    cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.Colspan = 2;
                    cell.PaddingTop = 5f;
                    cell.PaddingBottom = 5f;
                    cell.BackgroundColor = BaseColor.WHITE;
                    table.AddCell(cell);
                }

                document.Add(table);
            }

            if (model.Comments != null && model.Comments.Length > 0)
            {
                document.Add(new Paragraph(" "));
                document.Add(DrawUsersComment(model.Comments));
            }
        }

        private static void DrawFinancialInformation(PDFFlowCase model, Document document)
        {
            PdfPTable table = new PdfPTable(2);
            PdfPTable childTable = null;
            Phrase phrase = null;
            PdfPCell cell = null;

            Font fontTitle = FontFactory.GetFont("Arial", 13, Font.NORMAL, BaseColor.BLUE);
            Font fontTitlePanel = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);
            Font small = FontFactory.GetFont("Arial", 6, Font.NORMAL, BaseColor.BLACK);
            BaseColor color = new BaseColor(51, 122, 183);
            BaseColor headerColor = new BaseColor(127, 127, 127);

            phrase = new Phrase();
            phrase.Add(new Chunk("Financial Information", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = color;
            table.AddCell(cell);

            //Attachments
            if (model.Attachments != null && model.Attachments.Count() > 0)
            {
                for (int i = 0; i < model.Attachments.Length; i++)
                {
                    var item = model.Attachments[i];
                    var contenttype = GetContentType(item.FileName);
                    phrase = new Phrase();

                    if (contenttype != null && contenttype.StartsWith("image"))
                    {
                        System.Drawing.Image img = GetAttachmentImage(item.AttachementId);
                        if (img != null)
                        {
                            cell = ImageCell(img, 25f, PdfPCell.ALIGN_LEFT);
                            cell.BackgroundColor = BaseColor.WHITE;
                            cell.Colspan = 2;
                            cell.PaddingTop = 5f;
                            cell.PaddingLeft = 5f;
                            cell.PaddingBottom = 5f;
                            cell.PaddingRight = 5f;
                            table.AddCell(cell);
                        }
                        else
                        {
                            phrase.Add($"{(i + 1)}.{item.OriFileName}");
                            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                            cell.Colspan = 2;
                            cell.PaddingTop = 5f;
                            cell.PaddingLeft = 5f;
                            cell.PaddingBottom = 5f;
                            cell.PaddingRight = 5f;
                            cell.BackgroundColor = BaseColor.WHITE;
                        }
                    }
                    else
                    {
                        phrase.Add($"{(i + 1)}.{item.OriFileName}");
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.Colspan = 2;
                        cell.PaddingTop = 5f;
                        cell.PaddingLeft = 5f;
                        cell.PaddingBottom = 5f;
                        cell.PaddingRight = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                    }
                    table.AddCell(cell);
                }
            }

            //P&L Summary
            phrase = new Phrase();
            phrase.Add(new Chunk("P&L Summary", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BaseColor.WHITE;
            cell.BackgroundColor = headerColor;
            table.AddCell(cell);

            //Construction Cost Breakdown
            phrase = new Phrase();
            phrase.Add(new Chunk("Construction Cost Breakdown", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BaseColor.WHITE;

            cell.BackgroundColor = headerColor;
            table.AddCell(cell);

            // table of P&L Summary
            #region Add Child tbale P&L Summary
            var summaries = new[]
                                            {
                                                new KeyValuePair<string, string>("Sales", "PLSales"),
                                                new KeyValuePair<string, string>("Gross Profit", "PLGrossProfit"),
                                                new KeyValuePair<string, string>("Gross Margin", "PLGrossMargin"),
                                                new KeyValuePair<string, string>("Occupancy Costs", "PLOccupancyCosts"),
                                                new KeyValuePair<string, string>("Staff Salary", "PLStaffSalary"),
                                                new KeyValuePair<string, string>("Staff Commission", "PLStaffCommission"),
                                                new KeyValuePair<string, string>("Depreciation", "PLDepreciation"),
                                                new KeyValuePair<string, string>("Royalty", "PLRoyalty"),
                                                new KeyValuePair<string, string>("Other", "PLOther"),
                                                new KeyValuePair<string, string>("Total Operating Expenses", "PLTotalOperatingExpenses"),
                                                new KeyValuePair<string, string>("Store NOP", "PLStoreNOP")
                                            };

            childTable = new PdfPTable(new float[] { 49, 17, 17, 17 });
            for (int i = 0; i < 4; i++)
            {
                string header = "";
                if (i > 0)
                    header = $"Year {i}";
                phrase = new Phrase();
                phrase.Add(new Chunk(header, FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderColor = BaseColor.WHITE;
                cell.PaddingBottom = 5f;
                cell.PaddingLeft = 5f;
                cell.PaddingTop = 5f;
                cell.PaddingRight = 5f;
                childTable.AddCell(cell);
            }
            foreach (var item in summaries)
            {
                phrase = new Phrase();
                phrase.Add(new Chunk(item.Key, FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderColor = BaseColor.WHITE;
                cell.PaddingBottom = 5f;
                cell.PaddingLeft = 5f;
                cell.PaddingTop = 5f;
                cell.PaddingRight = 5f;
                childTable.AddCell(cell);
                for (int i = 1; i < 4; i++)
                {
                    var propValue = GetPropertyValueByPropertyName(model.PropertiesValue, (item.Value + i));
                    var floatValue = double.Parse(propValue ?? "0");
                    var displayValue = item.Value.Equals("PLGrossMargin") ? (floatValue > 0 ? floatValue + "%" : "") : floatValue.ToString("#,###");
                    phrase = new Phrase();
                    phrase.Add(new Chunk(displayValue, FontNormal));
                    cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.BackgroundColor = BaseColor.WHITE;
                    cell.BorderColor = BaseColor.WHITE;
                    cell.PaddingBottom = 5f;
                    cell.PaddingLeft = 5f;
                    cell.PaddingTop = 5f;
                    cell.PaddingRight = 5f;
                    childTable.AddCell(cell);
                }
            }

            cell = new PdfPCell(childTable);
            cell.Padding = 0;
            cell.BorderColor = BaseColor.WHITE;
            table.AddCell(cell);
            #endregion

            #region Construction Cost Breakdown
            var costs = new[]
                        {
                        new KeyValuePair<string, string>("Walls, Ceiling, & Floor", "WallsCeilingFloor"),
                        new KeyValuePair<string, string>("Furniture", "Furniture"),
                        new KeyValuePair<string, string>("Labor Cost", "LaborCost"),
                        new KeyValuePair<string, string>("IT Equipment", "ITEquipment"),
                        new KeyValuePair<string, string>("Utilities & Others", "Others"),
                        new KeyValuePair<string, string>("Moving, Assembly, Removal", "MovingAssemblyRemoval"),
                        new KeyValuePair<string, string>("Total Construction Cost", "TotalConstructionCost"),
                        new KeyValuePair<string, string>("3-Year Net Gain", "NetGain")
                    };

            childTable = new PdfPTable(new float[] { 50, 50 });
            for (int i = 0; i < 2; i++)
            {
                phrase = new Phrase();
                phrase.Add(new Chunk("", FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderColor = BaseColor.WHITE;
                cell.PaddingBottom = 5f;
                cell.PaddingLeft = 5f;
                cell.PaddingTop = 5f;
                cell.PaddingRight = 5f;
                childTable.AddCell(cell);
            }

            foreach (var item in costs)
            {
                phrase = new Phrase();
                phrase.Add(new Chunk(item.Key, FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderColor = BaseColor.WHITE;
                cell.PaddingBottom = 5f;
                cell.PaddingLeft = 5f;
                cell.PaddingTop = 5f;
                cell.PaddingRight = 5f;
                childTable.AddCell(cell);

                var propValue = GetPropertyValueByPropertyName(model.PropertiesValue, item.Value);
                var floatValue = double.Parse(propValue ?? "0");
                var displayValue = item.Value.Equals("PLGrossMargin") ? (floatValue > 0 ? floatValue + "%" : "") : floatValue.ToString("#,###");
                phrase = new Phrase();
                phrase.Add(new Chunk(displayValue, FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderColor = BaseColor.WHITE;
                cell.PaddingBottom = 5f;
                cell.PaddingLeft = 5f;
                cell.PaddingTop = 5f;
                cell.PaddingRight = 5f;
                childTable.AddCell(cell);
            }

            cell = new PdfPCell(childTable);
            cell.Padding = 0;
            cell.BorderColor = BaseColor.WHITE;
            table.AddCell(cell);
            #endregion

            #region Statistics
            phrase = new Phrase();
            phrase.Add(new Chunk("Statistics", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = headerColor;
            cell.BorderColor = BaseColor.WHITE;
            table.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BaseColor.WHITE;
            cell.BackgroundColor = headerColor;
            table.AddCell(cell);

            var SalesPerSqm = GetPropertyValueByPropertyName(model.PropertiesValue, "SalesPerSqm") ?? "0";
            var CAPEXPerSqm = GetPropertyValueByPropertyName(model.PropertiesValue, "CAPEXPerSqm") ?? "0";
            var SalesPerHeadcount = GetPropertyValueByPropertyName(model.PropertiesValue, "SalesPerHeadcount") ?? "0";
            var displayValue1 = double.Parse(SalesPerSqm).ToString("#,###.##");
            var displayValue2 = double.Parse(CAPEXPerSqm).ToString("#,###.##");
            var displayValue3 = double.Parse(SalesPerHeadcount).ToString("#,###.##");

            childTable = new PdfPTable(new float[] { 50, 5, 45 });
            cell = new PdfPCell();
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            childTable.AddCell(cell);
            childTable.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Within Guideline?", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            //Sales per Square Meter
            phrase = new Phrase();
            phrase.Add(new Chunk("Sales per Square Meter", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);
            //displayValue1
            phrase = new Phrase();
            phrase.Add(new Chunk(displayValue1, FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            //SalesPerSqmWithinGl
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "SalesPerSqmWithinGl"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            //CAPX per Square Meter
            phrase = new Phrase();
            phrase.Add(new Chunk("Sales per Square Meter", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);
            //displayValue2
            phrase = new Phrase();
            phrase.Add(new Chunk(displayValue2, FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            //CAPEXPerSqmWithinGl
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "CAPEXPerSqmWithinGl"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            //Sales per Headcount
            phrase = new Phrase();
            phrase.Add(new Chunk("Sales per Headcount", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);
            //displayValue3
            phrase = new Phrase();
            phrase.Add(new Chunk(displayValue3, FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            //SalesPerHeadcountWithinGl
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "SalesPerHeadcountWithinGl"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            //Expected Time to Breakeven
            phrase = new Phrase();
            phrase.Add(new Chunk("Expected Time to Breakeven", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);
            //ExpectedTimeToBreakeven
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ExpectedTimeToBreakeven"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            //ExpectedTimeToBreakevenWithinGl
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ExpectedTimeToBreakevenWithinGl"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);
            /**********************************/
            //Expected Time to CAPX Recovery
            phrase = new Phrase();
            phrase.Add(new Chunk("Expected Time to CAPX Recovery", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);
            //ExpectedTimeToCAPEXRecovery
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ExpectedTimeToCAPEXRecovery"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            //ExpectedTimeToCAPEXRecoveryWithinGl
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ExpectedTimeToCAPEXRecoveryWithinGl"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Kien test", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            cell = new PdfPCell(childTable);
            cell.Padding = 0;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            table.AddCell(cell);
            #endregion

            phrase = new Phrase();
            phrase.Add(new Chunk("", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.Padding = 0;
            cell.PaddingBottom = 10f;
            table.AddCell(cell);

            document.Add(table);

            table = new PdfPTable(1);
            table.PaddingTop = 10;
            phrase = new Phrase();
            phrase.Add(new Chunk("", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthTop = 1;
            cell.BorderColor = BaseColor.BLACK;
            cell.BorderWidthBottom = 1;
            table.AddCell(cell);
            document.Add(table);

            table = new PdfPTable(1);
            table.PaddingTop = 10;
            phrase = new Phrase();
            phrase.Add(new Chunk("Is this store in the annual budget?", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "WithinAnnualBudget"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthTop = 1;
            cell.BorderColor = BaseColor.WHITE;
            table.AddCell(cell);

            document.Add(table);
        }
        #endregion

        private static void DrawViewLeaveApplication(PDFFlowCase model, Document document)
        {
            PdfPTable table = new PdfPTable(2);
            PdfPCell cell = null;
            Phrase phrase = null;
            List<PdfPCell> cells = new List<PdfPCell>();
            Font fontTitle = FontFactory.GetFont("Arial", 13, Font.NORMAL, BaseColor.BLUE);
            var StaffNo = GetPropertyValueByPropertyName(model.PropertiesValue, "StaffNo");
            var FromDate = GetPropertyValueByPropertyName(model.PropertiesValue, "FromDate");
            var ToDate = GetPropertyValueByPropertyName(model.PropertiesValue, "ToDate");
            var FromTime = GetPropertyValueByPropertyName(model.PropertiesValue, "FromTime");
            var ToTime = GetPropertyValueByPropertyName(model.PropertiesValue, "ToTime");
            var TotalHours = GetPropertyValueByPropertyName(model.PropertiesValue, "TotalHours");

            //Version
            phrase = new Phrase();
            phrase.Add(new Chunk($"Version:  { model.FlowInfo.CaseInfo.Ver ?? 0}", fontTitle));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingBottom = 10f;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);

            //Country:
            phrase = new Phrase();
            phrase.Add(new Chunk("Country:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "Country"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);

            //Position:
            phrase = new Phrase();
            phrase.Add(new Chunk("Position:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "Position"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);
            //Applicant:
            phrase = new Phrase();
            phrase.Add(new Chunk("Applicant:  ", FontBold));
            phrase.Add(new Chunk(GetWF_UsernameByNo(StaffNo), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);

            //Staff no:
            phrase = new Phrase();
            phrase.Add(new Chunk("Staff No:  ", FontBold));
            phrase.Add(new Chunk(StaffNo, FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);
            document.Add(table);

            // Draw table Leave Information
            //Leave Information
            table = new PdfPTable(3);
            phrase = new Phrase();
            phrase.Add(new Chunk("Leave Information", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingBottom = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingRight = 5f;
            cell.BorderColorBottom = BaseColor.GRAY;
            cell.BorderWidthBottom = 1f;
            cell.Colspan = 3;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //From
            phrase = new Phrase();
            phrase.Add(new Chunk($"From:  ", FontBold));
            phrase.Add(new Chunk(FromDate, FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            table.AddCell(cell);

            //To
            phrase = new Phrase();
            phrase.Add(new Chunk("To:  ", FontBold));
            phrase.Add(new Chunk(ToDate, FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.Colspan = 2;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            table.AddCell(cell);

            //Time
            phrase = new Phrase();
            phrase.Add(new Chunk("Time:  ", FontBold));
            phrase.Add(new Chunk(FromTime, FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColorBottom = BaseColor.GRAY;
            cell.BorderWidthBottom = 1f;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cells.Add(cell);

            //Time
            phrase = new Phrase();
            phrase.Add(new Chunk("Time:  ", FontBold));
            phrase.Add(new Chunk(ToTime, FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BorderColorBottom = BaseColor.GRAY;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthBottom = 1f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            cells.Add(cell);

            //Total Day
            phrase = new Phrase();
            phrase.Add(new Chunk("Total Day:  ", FontBold));
            phrase.Add(new Chunk((String.IsNullOrEmpty(TotalHours) ? "0" : (double.Parse(TotalHours) / 8.0).ToString("0.##") + " days"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cells.Add(cell);
            cell.BorderColorBottom = BaseColor.GRAY;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthBottom = 1f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            table.Rows.Add(new PdfPRow(cells.ToArray()));

            //Leave Type:
            phrase = new Phrase();
            phrase.Add(new Chunk($"Leave Type:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "LeaveCode"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 3;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 10f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Remaining Balance:
            phrase = new Phrase();
            phrase.Add(new Chunk($"Remaining Balance:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "RemainingBalance"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BorderColorBottom = BaseColor.GRAY;
            cell.BorderWidthBottom = 1f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 10f;
            cell.Colspan = 3;
            table.AddCell(cell);

            //Reason for Leave:
            phrase = new Phrase();
            phrase.Add(new Chunk($"Reason for Leave:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ReasonForLeave"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.Colspan = 3;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 10f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            table.AddCell(cell);

            //Who will cover your duties?:
            phrase = new Phrase();
            var userNames = GetUsernames();
            var addedUserNames = model.FlowInfo.CaseInfo.CoverDuties.Select(n => userNames.FirstOrDefault(u => u.Key.Equals(n)).Value);
            phrase.Add(new Chunk($"Who will cover your duties?  ", FontBold));
            phrase.Add(new Chunk(String.Join(",", addedUserNames), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.Colspan = 3;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            table.AddCell(cell);

            #region Attachments
            phrase = new Phrase();
            phrase.Add(new Chunk("Attachments:  ", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            cell.Colspan = 3;
            table.AddCell(cell);
            document.Add(table);
            if (model.Attachments != null && model.Attachments.Length > 0)
            {
                table = new PdfPTable(1);

                for (int i = 0; i < model.Attachments.Length; i++)
                {
                    var attach = model.Attachments[i];
                    var contenttype = GetContentType(attach.FileName);
                    phrase = new Phrase();
                    phrase.Add(new Chunk($"{(i + 1)}.{attach.OriFileName}", FontNormal));
                    cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.BackgroundColor = BaseColor.WHITE;
                    cell.PaddingLeft = 5f;
                    cell.PaddingTop = 5f;
                    cell.PaddingRight = 5f;
                    cell.PaddingBottom = 5f;
                    table.AddCell(cell);
                    if (contenttype != null && contenttype.StartsWith("image"))
                    {
                        System.Drawing.Image img = GetAttachmentImage(attach.AttachementId);
                        if (img != null)
                        {
                            cell = ImageCell(img, 25f, PdfPCell.ALIGN_LEFT);
                            cell.BackgroundColor = BaseColor.WHITE;
                            cell.PaddingLeft = 5f;
                            cell.PaddingTop = 5f;
                            cell.PaddingRight = 5f;
                            cell.PaddingBottom = 5f;
                            table.AddCell(cell);
                        }
                    }
                }
                document.Add(table);
            }
            #endregion

            #region Comments
            if (model.Comments != null && model.Comments.Length > 0)
            {
                document.Add(new Paragraph(" "));
                table = DrawUsersComment(model.Comments);
                document.Add(table);
            }
            #endregion
        }

        private static void DrawViewStoreClosureForm(PDFFlowCase model, Document document)
        {
            var shopName = "";
            if (model.BLSShopViews != null)
            {
                var shopCode = GetPropertyValueByPropertyName(model.PropertiesValue, "Shop");
                shopName = model.BLSShopViews[shopCode];
            }

            PdfPTable table = new PdfPTable(1);
            PdfPTable childTable = null;
            Phrase phrase = null;
            PdfPCell cell = null;

            Font fontTitle = FontFactory.GetFont("Arial", 13, Font.NORMAL, BaseColor.BLUE);
            Font fontTitlePanel = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);
            BaseColor headerColor = new BaseColor(127, 127, 127);
            BaseColor color = new BaseColor(51, 122, 183);
            Phrase phraseBreakLine = new Phrase("\n");

            #region Header
            // Version
            phrase = new Phrase();
            phrase.Add(new Chunk($"Version:  { model.FlowInfo.CaseInfo.Ver ?? 0}", fontTitle));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingBottom = 10f;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);
            document.Add(table);
            #endregion

            document.Add(new Paragraph(" "));

            #region Store Search
            table = new PdfPTable(2);
            phrase = new Phrase();
            phrase.Add(new Chunk("Store Search", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 10f;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = color;
            table.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Brand:", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 10f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            var checkedBrand = GetPropertyValueByPropertyName(model.PropertiesValue, "Brand");
            if (!string.IsNullOrEmpty(checkedBrand))
            {
                if (BrandImages.ContainsKey(checkedBrand))
                    cell = ImageCell(BrandImages[checkedBrand], 25f, PdfPCell.ALIGN_CENTER);
                else
                    cell = new PdfPCell();
                cell.BorderColor = BaseColor.WHITE;
                cell.BackgroundColor = BaseColor.WHITE;
                table.AddCell(cell);
            }
            else
            {
                cell = new PdfPCell();
                cell.BorderColor = BaseColor.WHITE;
                cell.BackgroundColor = BaseColor.WHITE;
                table.AddCell(cell);
            }

            //Select a Shop:
            phrase = new Phrase();
            phrase.Add(new Chunk("Select a Shop:", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 10f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            //shopName
            phrase = new Phrase();
            phrase.Add(new Chunk(shopName, FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 10f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            document.Add(table);
            #endregion

            document.Add(new Paragraph(" "));
            #region Store Information
            table = new PdfPTable(1);
            phrase = new Phrase();
            phrase.Add(new Chunk("Store Information", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingLeft = 5;
            cell.PaddingTop = 10f;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = color;
            table.AddCell(cell);
            //Store Type
            phrase = new Phrase();
            phrase.Add(new Chunk("Store Type:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreType"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Store Name
            phrase = new Phrase();
            phrase.Add(new Chunk($"Store Name:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreName"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            childTable = new PdfPTable(4);
            #region childTable
            //City Tier:
            phrase = new Phrase();
            phrase.Add(new Chunk("City Tier:  ", FontNormal));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "CityTier"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            childTable.AddCell(cell);

            //Mall/Dept Store Tier:
            phrase = new Phrase();
            phrase.Add(new Chunk("Mall/Dept Store Tier:  ", FontNormal));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "MallDeptStoreTier"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            childTable.AddCell(cell);

            //Store Size (sq.m):
            phrase = new Phrase();
            phrase.Add(new Chunk("Store Size (sq.m):  ", FontNormal));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreSize"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            childTable.AddCell(cell);

            //# of Staff
            phrase = new Phrase();
            phrase.Add(new Chunk("# of Staff:  ", FontNormal));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StaffCount"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingBottom = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            childTable.AddCell(cell);
            #endregion

            cell = new PdfPCell(childTable);
            //cell.AddElement(childTable);
            cell.Padding = 0;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Shop Opening Date

            phrase = new Phrase();
            phrase.Add(new Chunk("Shop Opening Date: ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ShopOpeningDate"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            document.Add(table);
            #endregion
            document.Add(new Paragraph(" "));

            #region Key Contract Terms
            table = new PdfPTable(2);
            phrase = new Phrase();
            phrase.Add(new Chunk("Key Contract Terms", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 10f;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = color;
            table.AddCell(cell);

            //Name of Landlord/Operator
            phrase = new Phrase();
            phrase.Add(new Chunk("Name of Landlord/Operator:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "NameOfLandlord"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 15f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthBottom = 1f;
            cell.BorderColorBottom = BaseColor.BLACK;
            table.AddCell(cell);

            //Credit Terms(if applicable):
            phrase = new Phrase();
            phrase.Add(new Chunk("Credit Terms(if applicable):  ", FontBold));
            phrase.Add(new Chunk((GetPropertyValueByPropertyName(model.PropertiesValue, "CreditTerms") + " days"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 15f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthBottom = 1f;
            cell.BorderColorBottom = BaseColor.BLACK;
            table.AddCell(cell);

            //Contract Period
            phrase = new Phrase();
            phrase.Add(new Chunk("Contract Period:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ContractPeriodFrom"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //To
            phrase = new Phrase();
            phrase.Add(new Chunk("To:  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ContractPeriodTo"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);


            //Rental/Lease Amount
            phrase = new Phrase();
            phrase.Add(new Chunk("Rental/Lease Amount", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "FirstRenewalPeriodTo"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            #region Chirlden Rental/Lease Amount            
            childTable = new PdfPTable(new float[] { 50, 50 });
            var CurrentTermType = GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermType");
            var rentFlag = CurrentTermType.EqualsIgnoreCaseAndBlank("Fixed Rent");
            var turnoverFlag = CurrentTermType.EqualsIgnoreCaseAndBlank("Turnover % with Minimum Fixed Rent");
            var FixedRentFlag = CurrentTermType.EqualsIgnoreCaseAndBlank("Fixed Rent + Turnover %");

            phrase = new Phrase();
            phrase.Add(new Chunk("", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.BorderWidth = 1;
            cell.BorderColor = BaseColor.GRAY;
            childTable.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Current Term", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.BorderWidth = 1;
            cell.BorderColor = BaseColor.GRAY;
            childTable.AddCell(cell);

            //Fixed Rent
            phrase = new Phrase();
            phrase.Add(new Chunk("Fixed Rent", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.BorderWidth = 1;
            cell.BorderColor = BaseColor.GRAY;
            childTable.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Rent    ", FontNormal));
            phrase.Add(new Chunk((rentFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermRent") : ""), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.BorderWidth = 1;
            cell.BorderColor = BaseColor.GRAY;
            childTable.AddCell(cell);

            //Turnover % with Minimum Fixed Rent
            phrase = new Phrase();
            phrase.Add(new Chunk("Turnover % with Minimum Fixed Rent", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.BorderWidth = 1;
            cell.BorderColor = BaseColor.GRAY;
            childTable.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Turnover %   ", FontNormal));
            phrase.Add(new Chunk((turnoverFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermTurnover") : ""), FontNormal));
            phrase.Add(phraseBreakLine);
            phrase.Add(new Chunk("Rent   ", FontNormal));
            phrase.Add(new Chunk((turnoverFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermRent") : ""), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.BorderWidth = 1;
            cell.BorderColor = BaseColor.GRAY;
            childTable.AddCell(cell);

            //Fixed Rent + Turnover %
            phrase = new Phrase();
            phrase.Add(new Chunk("Fixed Rent + Turnover %", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.BorderWidth = 1;
            cell.BorderColor = BaseColor.GRAY;
            childTable.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Turnover %   ", FontNormal));
            phrase.Add(new Chunk((FixedRentFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermTurnover") : ""), FontNormal));
            phrase.Add(phraseBreakLine);
            phrase.Add(new Chunk("Rent   ", FontNormal));
            phrase.Add(new Chunk((FixedRentFlag ? GetPropertyValueByPropertyName(model.PropertiesValue, "CurrentTermRent") : ""), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.BorderWidth = 1;
            cell.BorderColor = BaseColor.GRAY;
            childTable.AddCell(cell);

            cell = new PdfPCell(childTable);
            //cell.AddElement(childTable);
            cell.Colspan = 2;
            cell.Padding = 10;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            table.AddCell(cell);
            #endregion

            //Deposit/Key Money:
            phrase = new Phrase();
            phrase.Add(new Chunk("Deposit/Key Money: ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "DepositMoney"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColorTop = BaseColor.BLACK;
            cell.BorderWidthTop = 1;

            table.AddCell(cell);
            //Refundable
            phrase = new Phrase();
            phrase.Add(new Chunk("Premium(if applicable): ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "Premium"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Refundable Amount:
            phrase = new Phrase();
            phrase.Add(new Chunk("Refundable Amount::  ", FontBold));
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "RefundableAmount"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            document.Add(table);
            #endregion

            document.Add(new Paragraph(" "));

            #region Closure Information
            table = new PdfPTable(new float[] { 70, 30 });
            phrase = new Phrase();
            phrase.Add(new Chunk("Closure Information", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.Colspan = 2;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 10f;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = color;
            table.AddCell(cell);

            //Store Closure Date:
            phrase = new Phrase();
            phrase.Add(new Chunk("Store Closure Date:", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 10f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Store Closure Date:
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "StoreClosureDate"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 10f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);


            //Reason for Closure:
            phrase = new Phrase();
            phrase.Add(new Chunk("Reason for Closure:", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 10f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthBottom = 1f;
            cell.BorderColorBottom = BaseColor.BLACK;
            table.AddCell(cell);

            //Reason for Closure::
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "ReasonForClosure"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 10f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderWidthBottom = 1f;
            cell.BorderColorBottom = BaseColor.BLACK;
            table.AddCell(cell);

            var LossOnDisposal = Double.Parse(GetPropertyValueByPropertyName(model.PropertiesValue, "LossOnDisposal") ?? "0");
            var ReinstatementCost = Double.Parse(GetPropertyValueByPropertyName(model.PropertiesValue, "ReinstatementCost") ?? "0");
            var LeaseTerminationCost = Double.Parse(GetPropertyValueByPropertyName(model.PropertiesValue, "LeaseTerminationCost") ?? "0");
            var StaffTerminationCost = Double.Parse(GetPropertyValueByPropertyName(model.PropertiesValue, "StaffTerminationCost") ?? "0");
            var total = LossOnDisposal + ReinstatementCost + LeaseTerminationCost + StaffTerminationCost;
            //Loss on Disposal
            phrase = new Phrase();
            phrase.Add(new Chunk("Loss on Disposal:", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            // 
            phrase = new Phrase();
            phrase.Add(new Chunk(LossOnDisposal.ToString("#,###.##"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);


            //Reinstatement Cost
            phrase = new Phrase();
            phrase.Add(new Chunk("Reinstatement Cost:", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            // 
            phrase = new Phrase();
            phrase.Add(new Chunk(ReinstatementCost.ToString("#,###.##"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);


            //Lease Termination Cost(if applicable):
            phrase = new Phrase();
            phrase.Add(new Chunk("Lease Termination Cost(if applicable):", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            // 
            phrase = new Phrase();
            phrase.Add(new Chunk(LeaseTerminationCost.ToString("#,###.##"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Staff Termination Cost(if applicable)
            phrase = new Phrase();
            phrase.Add(new Chunk("Staff Termination Cost(if applicable):", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            // 
            phrase = new Phrase();
            phrase.Add(new Chunk(StaffTerminationCost.ToString("#,###.##"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);


            //Total Cost of Closure
            phrase = new Phrase();
            phrase.Add(new Chunk("Total Cost of Closure:", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            // 
            phrase = new Phrase();
            phrase.Add(new Chunk(total.ToString("#,###.##"), FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 10f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            document.Add(table);
            #endregion

            document.Add(new Paragraph(" "));

            #region Financial Information
            table = new PdfPTable(1);

            phrase = new Phrase();
            phrase.Add(new Chunk("Financial Information", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 10f;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = color;
            table.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BorderColor = BaseColor.WHITE;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //P&L Summary
            phrase = new Phrase();
            phrase.Add(new Chunk("P&L Summary", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 10f;
            cell.PaddingRight = 10f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BaseColor.WHITE;
            cell.BackgroundColor = headerColor;
            table.AddCell(cell);


            #region childTable
            childTable = new PdfPTable(3);
            var summaries = new[]
                                {
                                    new KeyValuePair<string, string>("Sales", "PLSales"),
                                    new KeyValuePair<string, string>("Gross Profit", "PLGrossProfit"),
                                    new KeyValuePair<string, string>("Occupancy Costs", "PLOccupancyCosts"),
                                    new KeyValuePair<string, string>("Staff Salary", "PLStaffSalary"),
                                    new KeyValuePair<string, string>("Depreciation", "PLDepreciation"),
                                    new KeyValuePair<string, string>("Royalty", "PLRoyalty"),
                                    new KeyValuePair<string, string>("Other", "PLOther"),
                                    new KeyValuePair<string, string>("Total Operating Expenses", "PLTotalOperatingExpenses"),
                                    new KeyValuePair<string, string>("Store NOP", "PLStoreNOP")
                                };
            phrase = new Phrase();
            phrase.Add(new Chunk("", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.BorderColor = BaseColor.WHITE;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Past 12 Months", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = headerColor;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Year1 (if renewed)", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = headerColor;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            childTable.AddCell(cell);

            foreach (var item in summaries)
            {
                phrase = new Phrase();
                phrase.Add(new Chunk(item.Key, FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.BackgroundColor = BaseColor.WHITE;
                cell.BorderColor = BaseColor.WHITE;
                cell.PaddingBottom = 5f;
                cell.PaddingLeft = 5f;
                cell.PaddingTop = 5f;
                cell.PaddingRight = 5f;
                childTable.AddCell(cell);
                for (int i = 1; i <= 2; i++)
                {
                    var propValue = GetPropertyValueByPropertyName(model.PropertiesValue, (item.Value + i));
                    var floatValue = double.Parse(propValue ?? "0");

                    phrase = new Phrase();
                    phrase.Add(new Chunk(floatValue.ToString("#,###.##"), FontNormal));
                    cell = PhraseCell(phrase, PdfPCell.ALIGN_RIGHT);
                    cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                    cell.BackgroundColor = BaseColor.WHITE;
                    cell.BorderColor = BaseColor.WHITE;
                    cell.PaddingBottom = 5f;
                    cell.PaddingLeft = 5f;
                    cell.PaddingTop = 5f;
                    cell.PaddingRight = 5f;
                    childTable.AddCell(cell);
                }
            }

            cell = new PdfPCell(childTable);
            cell.Padding = 0;
            cell.BorderColor = BaseColor.WHITE;
            table.AddCell(cell);
            #endregion
            document.Add(table);
            #endregion

            document.Add(new Paragraph(" "));

            #region Other Information 

            table = new PdfPTable(1);
            phrase = new Phrase();
            phrase.Add(new Chunk("Other Information", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 10f;
            cell.PaddingBottom = 10f;
            cell.BackgroundColor = color;
            table.AddCell(cell);

            //OtherInformation
            phrase = new Phrase();
            phrase.Add(new Chunk(GetPropertyValueByPropertyName(model.PropertiesValue, "OtherInformation"), FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingTop = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingBottom = 5f;
            cell.PaddingRight = 5f;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            document.Add(table);
            #endregion

            if (model.Comments != null && model.Comments.Length > 0)
            {
                document.Add(new Paragraph(" "));
                document.Add(DrawUsersComment(model.Comments));
            }
        }

        private static void DrawViewProperties(PDFFlowCase model, Document document)
        {
            PdfPTable table = new PdfPTable(1);
            Phrase phrase = null;
            PdfPCell cell = null;

            Font fontTitle = FontFactory.GetFont("Arial", 13, Font.NORMAL, BaseColor.BLUE);
            Font fontTitlePanel = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);

            BaseColor headerColor = new BaseColor(127, 127, 127);

            BaseColor color = new BaseColor(221, 221, 221);
            Phrase phraseBreakLine = new Phrase("\n");
            #region Header
            // Version
            phrase = new Phrase();
            phrase.Add(new Chunk($"Version:  { model.FlowInfo.CaseInfo.Ver ?? 0}", fontTitle));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingBottom = 15f;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);
            document.Add(table);
            #endregion

            #region Content 
            table = new PdfPTable(3);

            phrase = new Phrase();
            phrase.Add(new Chunk("Subject", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.BLACK;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.BorderWidthRight = 1;
            cell.BorderColorRight = color;
            table.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Department:", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.BLACK;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.BorderWidthRight = 1;
            cell.BorderColorRight = color;
            table.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk("Deadline", fontTitlePanel));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.BLACK;
            cell.PaddingBottom = 5f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;

            table.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk(model.FlowInfo.CaseInfo.Subject, FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 20f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 20f;
            cell.PaddingRight = 5f;
            cell.BorderWidthRight = 1;
            cell.BorderColorRight = color;
            table.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk(model.FlowInfo.CaseInfo.Department, FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 20f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 20f;
            cell.PaddingRight = 5f;
            cell.BorderWidthRight = 1;
            cell.BorderColorRight = color;
            table.AddCell(cell);

            phrase = new Phrase();
            phrase.Add(new Chunk((model.FlowInfo.CaseInfo.Deadline?.ToLocalTime().ToString("M/d/yyyy") ?? string.Empty), FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingBottom = 20f;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 20f;
            cell.PaddingRight = 5f;

            table.AddCell(cell);

            if (model.PropertiesValue != null && model.PropertiesValue.PropertyInfo.Any() == true)
            {
                var result = SplitToGroup(model.PropertiesValue.PropertyInfo, 3);

                foreach (var props in result)
                {
                    List<PdfPCell> cellHeaders = new List<PdfPCell>();
                    List<PdfPCell> cellContents = new List<PdfPCell>();
                    foreach (var prop in props)
                    {
                        WF_CasePropertyValues value = model.PropertiesValue.Values.FirstOrDefault(p => p.PropertyId == prop.FlowPropertyId);
                        var display = "";
                        if (value != null)
                        {

                            if (prop.PropertyType == 10)
                            {
                                display = Roles.ContainsKey(value.StringValue ?? string.Empty) ? Roles[value.StringValue ?? string.Empty] : value.StringValue;
                            }
                            else if (prop.PropertyType == 11)
                            {
                                display = Departments.ContainsKey(value.StringValue ?? string.Empty) ? Departments[value.StringValue ?? string.Empty] : value.StringValue;
                            }
                            else if (prop.PropertyType == 12)
                            {
                                display = DeptTypes.ContainsKey(value.StringValue ?? string.Empty) ? DeptTypes[value.StringValue ?? string.Empty] : value.StringValue;
                            }
                            else if (prop.PropertyType == 14)
                            {
                                display = GetBrandFullName(value.StringValue ?? string.Empty);
                            }
                            else
                            {
                                display = (value?.StringValue
                                          ?? value?.IntValue?.ToString()
                                          ?? value?.DateTimeValue?.ToString("yyyy-MM-dd HH:mm")
                                          ?? value?.NumericValue?.ToString("f2")
                                          ?? value?.TextValue
                                          ?? value?.DateValue?.ToString("yyyy-MM-dd HH:mm")
                                          ?? GetWF_UsernameByNo(value?.UserNoValue));
                            }
                        }


                        phrase = new Phrase();
                        phrase.Add(new Chunk(prop.PropertyName, fontTitlePanel));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.BackgroundColor = BaseColor.BLACK;
                        cell.PaddingBottom = 5f;
                        cell.PaddingLeft = 5f;
                        cell.PaddingTop = 5f;
                        cell.PaddingRight = 5f;

                        if (cellHeaders.Count < 3)
                        {
                            cell.BorderWidthRight = 1;
                            cell.BorderColorRight = color;
                        }
                        cellHeaders.Add(cell);

                        phrase = new Phrase();
                        phrase.Add(new Chunk(display, FontBold));
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_CENTER);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.BackgroundColor = BaseColor.WHITE;
                        cell.PaddingBottom = 20f;
                        cell.PaddingLeft = 5f;
                        cell.PaddingTop = 20f;
                        cell.PaddingRight = 5f;
                        if (cellHeaders.Count < 3)
                        {
                            cell.BorderWidthRight = 1;
                            cell.BorderColorRight = color;
                        }
                        cellContents.Add(cell);
                    }
                    table.Rows.Add(new PdfPRow(cellHeaders.ToArray()));
                    table.Rows.Add(new PdfPRow(cellContents.ToArray()));
                }
            }

            document.Add(table);
            #endregion

            document.Add(new Paragraph(" "));

            table = new PdfPTable(1);
            phrase = new Phrase();
            phrase.Add(new Chunk("Attachments:  ", FontBold));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            table.AddCell(cell);

            //Attachments
            if (model.Attachments != null && model.Attachments.Count() > 0)
            {
                for (int i = 0; i < model.Attachments.Length; i++)
                {
                    var item = model.Attachments[i];
                    var contenttype = GetContentType(item.FileName);
                    phrase = new Phrase();

                    if (contenttype != null && contenttype.StartsWith("image"))
                    {
                        System.Drawing.Image img = GetAttachmentImage(item.AttachementId);
                        if (img != null)
                        {
                            cell = ImageCell(img, 25f, PdfPCell.ALIGN_LEFT);
                            cell.BackgroundColor = BaseColor.WHITE;
                            cell.PaddingTop = 5f;
                            cell.PaddingLeft = 5f;
                            cell.PaddingBottom = 5f;
                            cell.PaddingRight = 5f;
                            table.AddCell(cell);
                        }
                        else
                        {
                            phrase.Add($"{(i + 1)}.{item.OriFileName}");
                            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                            cell.PaddingTop = 5f;
                            cell.PaddingLeft = 5f;
                            cell.PaddingBottom = 5f;
                            cell.PaddingRight = 5f;
                            cell.BackgroundColor = BaseColor.WHITE;
                        }
                    }
                    else
                    {
                        phrase.Add($"{(i + 1)}.{item.OriFileName}");
                        cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                        cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                        cell.PaddingTop = 5f;
                        cell.PaddingLeft = 5f;
                        cell.PaddingBottom = 5f;
                        cell.PaddingRight = 5f;
                        cell.BackgroundColor = BaseColor.WHITE;
                    }
                    table.AddCell(cell);
                }
            }

            document.Add(table);

            document.Add(new Paragraph(" "));

            if (model.Comments != null && model.Comments.Length > 0)
            {
                document.Add(new Paragraph(" "));
                document.Add(DrawUsersComment(model.Comments));
            }
        }

        private static PdfPTable DrawUsersComment(CaseNotification[] comments)
        {
            PdfPTable table = new PdfPTable(1);
            Phrase phrase = null;
            PdfPCell cell = null;
            phrase = new Phrase();
            //Comments
            phrase = new Phrase();
            phrase.Add(new Chunk("Comments:", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.BackgroundColor = BaseColor.WHITE;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BackgroundColor;
            table.AddCell(cell);

            foreach (var msg in comments)
            {
                phrase = new Phrase();
                phrase.Add(new Chunk($"{GetWF_UsernameByNo(msg.Sender)} Commented on {msg.Created.ToString("M/d/yyyy h:m:s tt")} </br> {msg.Comments}", FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.PaddingLeft = 5f;
                cell.PaddingTop = 5f;
                cell.PaddingRight = 5f;
                cell.PaddingBottom = 5f;
                cell.BorderColor = BackgroundColor;
                table.AddCell(cell);
            }

            return table;
        }

        private static PdfPTable DrawCaseLogs(CaseLog[] caseLogs)
        {
            PdfPTable table = new PdfPTable(new float[] { 25, 25, 75 });
            Phrase phrase = null;
            PdfPCell cell = null;
            phrase = new Phrase();

            //Time
            phrase = new Phrase();
            phrase.Add(new Chunk("Time", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BackgroundColor;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Date:
            phrase = new Phrase();
            phrase.Add(new Chunk("Date", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BackgroundColor;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);

            //Description:
            phrase = new Phrase();
            phrase.Add(new Chunk("Description", FontNormal));
            cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.PaddingLeft = 5f;
            cell.PaddingTop = 5f;
            cell.PaddingRight = 5f;
            cell.PaddingBottom = 5f;
            cell.BorderColor = BackgroundColor;
            cell.BackgroundColor = BaseColor.WHITE;
            table.AddCell(cell);
            foreach (var group in caseLogs.GroupBy(p => new { p.LogType, p.MessageId, p.MessageTypeId }))
            {
                var item = group.First();
                string usernameByNo = string.Join(", ", group.Select(p => GetWF_UsernameByNo(p.ReceiverUser)));
                string sender = GetWF_UsernameByNo(item.SenderUser);

                phrase = new Phrase();
                phrase.Add(new Chunk(item.Created.ToLocalTime().ToString("HH:mm"), FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.PaddingLeft = 5f;
                cell.PaddingTop = 5f;
                cell.PaddingRight = 5f;
                cell.PaddingBottom = 5f;
                cell.BorderColor = BackgroundColor;
                cell.BackgroundColor = BaseColor.WHITE;
                table.AddCell(cell);


                phrase = new Phrase();
                phrase.Add(new Chunk(item.Created.ToLocalTime().ToString("MM/dd/yyyy"), FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.PaddingLeft = 5f;
                cell.PaddingTop = 5f;
                cell.PaddingRight = 5f;
                cell.PaddingBottom = 5f;
                cell.BorderColor = BackgroundColor;
                cell.BackgroundColor = BaseColor.WHITE;
                table.AddCell(cell);

                string result = "";
                if (item.LogType == "Notifications")
                {
                    if (item.MessageTypeId == (int)NotificationSources.ApproverCommentted)
                        result = $"{sender} has commentted on the application.";
                    else if (item.MessageTypeId == (int)NotificationSources.ApproverAbort)
                        result = $"{sender} has commentted on the application.";
                    else if (item.MessageTypeId == (int)NotificationSources.ApproverRejected)
                        result = $"{sender} has commentted on the application.";
                    else if (item.MessageTypeId == (int)NotificationSources.StepNotificateUser || item.MessageTypeId == (int)NotificationSources.ApplicantNotificateUser || item.MessageTypeId == (int)NotificationSources.LastStepNotificateUser)
                        result = $"{usernameByNo} has been notified";
                    else if (item.MessageTypeId == (int)NotificationSources.FinalNotifyUser)
                        result = $"Application has been sent to final notification user {usernameByNo}";
                    else if (item.MessageTypeId == (int)NotificationSources.CoverPerson)
                        result = $"Application has been sent to covered person {usernameByNo}";
                }
                else
                {
                    if (item.MessageTypeId == (int)CaseLogType.AppAssignApprover)
                        result = $"Application is with{usernameByNo}";
                    else if (item.MessageTypeId == (int)CaseLogType.AppViewed)
                        result = $"Application has been read by {usernameByNo}";
                    else if (item.MessageTypeId == (int)CaseLogType.AppStepApproved)
                        result = $"Application is approved by {usernameByNo}";
                    else if (item.MessageTypeId == (int)CaseLogType.AppStepRejected)
                        result = $"Application is rejected by {usernameByNo}";
                    else if (item.MessageTypeId == (int)CaseLogType.AppStepAbort)
                        result = $"Application is send back to revise by {usernameByNo}";
                    else if (item.MessageTypeId == (int)CaseLogType.AppCancelled)
                        result = $"Application is cancelled by {usernameByNo}";
                    else if (item.MessageTypeId == (int)CaseLogType.AppCreated)
                        result = $"Application was successfully submitted {usernameByNo}";
                    else if (item.MessageTypeId == (int)CaseLogType.AppRevised)
                        result = $"Application is revised by {usernameByNo} to a new version.";
                }
                phrase = new Phrase();
                phrase.Add(new Chunk(result, FontNormal));
                cell = PhraseCell(phrase, PdfPCell.ALIGN_LEFT);
                cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
                cell.PaddingLeft = 5f;
                cell.PaddingTop = 5f;
                cell.PaddingRight = 5f;
                cell.PaddingBottom = 5f;
                cell.BorderColor = BackgroundColor;
                cell.BackgroundColor = BaseColor.WHITE;
                table.AddCell(cell);

            }
            return table;
        }
        #endregion

        #region Draw Helper
        private static System.Drawing.Image GetImage(string imageName)
        {
            System.Reflection.Assembly thisExe;
            thisExe = System.Reflection.Assembly.GetExecutingAssembly();
            System.IO.Stream file = thisExe.GetManifestResourceStream($"Omnibackend.Workflow.Images.{imageName}.png");

            return System.Drawing.Image.FromStream(file);
        }

        private static System.Drawing.Image GetImage(byte[] bytes)
        {
            var ms = new System.IO.MemoryStream(bytes);
            var img = System.Drawing.Image.FromStream(ms);
            return img;
        }

        private static System.Drawing.Image GetAttachmentImage(int attachmentId)
        {
            using (WorkFlowApiClient client = new WorkFlowApiClient())
            {
                var contentType = "";
                byte[] bytes = client.GetImageFile(attachmentId, out contentType);
                if (contentType.StartsWith("image"))
                    return GetImage(bytes);
                return null;
            }
        }

        private static PdfPCell PhraseCell(Phrase phrase, int align)
        {
            PdfPCell cell = new PdfPCell(phrase);
            cell.BorderColor = BaseColor.WHITE;
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.HorizontalAlignment = align;
            cell.PaddingBottom = 2f;
            cell.PaddingTop = 0f;
            return cell;
        }

        private static PdfPCell ImageCell(string imageName, float scale, int align)
        {
            PdfPCell cell;
            if (string.IsNullOrEmpty(imageName))
                cell = new PdfPCell();
            else
            {
                iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(GetImage(imageName), ImageFormat.Png);
                image.ScalePercent(scale);
                cell = new PdfPCell(image);
            }
            cell.BorderColor = BaseColor.WHITE;
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.HorizontalAlignment = align;
            cell.PaddingBottom = 0f;
            cell.PaddingTop = 0f;
            return cell;
        }

        private static PdfPCell ImageCell(System.Drawing.Image img, float scale, int align)
        {
            PdfPCell cell;
            iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(img, ImageFormat.Png);
            image.ScalePercent(scale);
            cell = new PdfPCell(image);

            cell.BorderColor = BaseColor.WHITE;
            cell.VerticalAlignment = PdfPCell.ALIGN_TOP;
            cell.HorizontalAlignment = align;
            cell.PaddingBottom = 0f;
            cell.PaddingTop = 0f;
            return cell;
        }

        private static PdfPTable DrawLine()
        {
            PdfPTable table = new PdfPTable(1);
            PdfPCell cell = null;
            cell = PhraseCell(new Phrase(), PdfPCell.ALIGN_LEFT);
            cell.Border = 0;
            cell.BorderWidthBottom = 1f;
            cell.BorderColor = BaseColor.DARK_GRAY;
            table.AddCell(cell);
            return table;
        }
        #endregion

        #region Cache
        static readonly object _GlobalUserViewsLock = new object();
        static GlobalUserView[] _GlobalUserViews;
        static GlobalUserView[] GlobalUserViews
        {
            get
            {
                lock (_GlobalUserViewsLock)
                {
                    if (_GlobalUserViews == null)
                    {
                        lock (_GlobalUserViewsLock)
                        {
                            using (WorkFlowEntities entities =  new WorkFlowEntities())
                            {
                                _GlobalUserViews = entities.GlobalUserView.ToArray();
                            }
                        }
                    }
                }
                return _GlobalUserViews;
            }
        }

        private static string GetWF_UsernameByNo(string userno)
        {
            var user = GlobalUserViews.FirstOrDefault(p => p.EmployeeID.EqualsIgnoreCaseAndBlank(userno));
            string username = (user != null) ? user.EmployeeName : userno;
            return username;
        }
        private static Dictionary<string, string> GetUsernames()
        {
            if (GlobalUserViews == null)
                return new Dictionary<string, string>();

            return GlobalUserViews.Select(p => new { p.EmployeeID, p.EmployeeName }).GroupBy(p => p.EmployeeID)
                            .ToDictionary(p => p.Key, p => p.First().EmployeeName);
        }

        static readonly object _RolesLock = new object();
        static Dictionary<string, string> _Roles;
        static Dictionary<string, string> Roles
        {
            get
            {
                lock (_RolesLock)
                {
                    if (_Roles == null)
                    {
                        lock (_RolesLock)
                        {
                            using (ApiClient client = new ApiClient())
                            {
                                var result = client.MasterCode_Search(new MasterCodeSearchParams
                                {
                                    country = Consts.GetApiCountry(),
                                    id = "ZAROLE0",
                                    language = "ENG",
                                    code = "%"
                                });

                                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                                    _Roles = result.ReturnValue.ToDictionary(k => k.ZZ03_CODE, v => v.ZZ03_CDC1);
                                else
                                    _Roles = new Dictionary<string, string>();
                            }
                        }
                    }
                }
                return _Roles;
            }
        }

        static readonly object _DepartmentsLock = new object();
        static Dictionary<string, string> _Departments;
        static Dictionary<string, string> Departments
        {
            get
            {
                lock (_DepartmentsLock)
                {
                    if (_Departments == null)
                    {
                        lock (_DepartmentsLock)
                        {
                            using (ApiClient client = new ApiClient())
                            {
                                var result = client.MasterCode_Search(new MasterCodeSearchParams
                                {
                                    country = Consts.GetApiCountry(),
                                    id = "ZADEPT1",
                                    language = "ENG",
                                    code = "%"
                                });
                                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                                    _Departments = result.ReturnValue.ToDictionary(k => k.ZZ03_CODE, v => v.ZZ03_CDC1);
                                else
                                    _Departments = new Dictionary<string, string>();
                            }
                        }
                    }
                }
                return _Departments;
            }
        }

        static readonly object _DeptTypesLock = new object();
        static Dictionary<string, string> _DeptTypes;
        static Dictionary<string, string> DeptTypes
        {
            get
            {
                lock (_DeptTypesLock)
                {
                    if (_DeptTypes == null)
                    {
                        lock (_DeptTypesLock)
                        {
                            using (ApiClient client = new ApiClient())
                            {
                                var result = client.MasterCode_Search(new MasterCodeSearchParams
                                {
                                    country = Consts.GetApiCountry(),
                                    id = "ZADEPT0",
                                    language = "ENG",
                                    code = "%"
                                });

                                if (string.IsNullOrWhiteSpace(result.ErrorMessage) && result.ReturnValue != null)
                                    _DeptTypes = result.ReturnValue.ToDictionary(k => k.ZZ03_CODE, v => v.ZZ03_CDC1);

                                _DeptTypes = new Dictionary<string, string>();
                            }
                        }
                    }
                }
                return _DeptTypes;
            }
        }

        private static string GetBrandFullName(string brand)
        {
            if (brand.EqualsIgnoreCaseAndBlank("ROS"))
                return "ROOTS";
            if (brand.EqualsIgnoreCaseAndBlank("HTN"))
                return "HangTen ";
            if (brand.EqualsIgnoreCaseAndBlank("HCT"))
                return "H:Connect";
            if (brand.EqualsIgnoreCaseAndBlank("APM"))
                return "Arnold Palmer";
            if (brand.EqualsIgnoreCaseAndBlank("LEO"))
                return "LEO";
            return brand;
        }
        #endregion

        #region Utils
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
                return "text/" + ext.Trim('.'); ;
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

        private static string GetPropertyValueByPropertyName(PropertiesValue properties, string propertyName)
        {
            WF_FlowPropertys prop = properties.PropertyInfo.FirstOrDefault(p => p.StatusId < 0 && p.PropertyName.Equals(propertyName));
            return GetPropertyValue(properties, prop);
        }

        private static string GetPropertyValue(PropertiesValue properties, WF_FlowPropertys prop)
        {
            if (prop == null)
            {
                return null;
            }
            WF_CasePropertyValues value = properties.Values.FirstOrDefault(p => p.PropertyId == prop.FlowPropertyId);
            return value?.StringValue
                ?? value?.IntValue?.ToString()
                ?? value?.DateTimeValue?.ToString("yyyy-MM-ddTHH:mm")
                ?? value?.NumericValue?.ToString("0.##")
                ?? value?.TextValue
                ?? value?.DateValue?.ToString("M/dd/yyyy")
                ?? value?.UserNoValue;
        }

        private static bool hideControls(PropertiesValue properties, string controlNames)
        {
            var names = controlNames.Split(',');
            var count = names.Count(name => properties.PropertyInfo.Count(p => p.PropertyName == name && p.StatusId < 0) > 0);
            return count > 0;
        }

        private static bool hideControl(PropertiesValue properties, string name)
        {
            return properties.PropertyInfo.Count(p => p.PropertyName.Equals(name) && p.StatusId < 0) > 0;
        }

        private static IEnumerable<List<T>> SplitToGroup<T>(IEnumerable<T> source, int count)
        {
            List<T> target = new List<T>();
            foreach (T item in source)
            {
                target.Add(item);
                if (target.Count == count)
                {
                    yield return new List<T>(target);
                    target.Clear();
                }
            }
            if (target.Count > 0)
                yield return target;
        }
        #endregion
    }
}
