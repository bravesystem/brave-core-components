using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace BRaVe_Portal.Pages.Assessments
{
    public class ReadonlyAssessmentModel : PageModel
    {

        private readonly ILogger<IndexModel> _logger;

        private readonly ILookupService _lookupService;

        private readonly IRBAHelperService _rBAHelperService;

        private readonly IBlobStorageService _blobStorage;

        private const string RBA_ATTACCHMENTS = "rba-attachments";

        private readonly string languageCode;

        public ReadonlyAssessmentModel(ILogger<IndexModel> logger, ILookupService lookupService, IRBAHelperService rBAHelperService, IBlobStorageService blobStorage)
        {
            _logger = logger;
            _lookupService = lookupService;
            _rBAHelperService = rBAHelperService;
            _blobStorage = blobStorage;

            languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }


        public RiskBenefitAssessmentViewModel Assessment { get; set; } = new();
        public string ProgramManagerName { get; set; }

        public string ProgramManagerContacts { get; set; }

        public async Task<IActionResult> OnGet(int assessmentId)
        {

            Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode);

            ViewData["IsReadOnly"] = true; //Assessment.IsReadOnly || User.IsInRole(EnumUserRoles.PM.ToString());

            ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
            ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

            ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

            CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

            ViewData["CoreLookups"] = coreLookups;

            return Page();
        }

        //DOWNLOAD HANDLER FOR THE MOCKED AZURE STORAGE
        public async Task<IActionResult> OnGetDownloadAttachmentAsync(string blobPath)

        {
            try
            {
                var file = await _blobStorage.ReadAsync(
                    containerName: RBA_ATTACCHMENTS,
                    blobPath: blobPath);


                var bytes = file.Content.ToArray();
                var contentType = file.Details.ContentType ?? "application/octet-stream";
                var downloadFileName = Path.GetFileName(blobPath); // e.g., "doc.pdf"


                return File(
                    bytes,
                    contentType,
                    downloadFileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound("Attachment not found.");
            }
        }

    }
}
