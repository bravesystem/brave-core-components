using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Portal.Controllers
{
    public class EnrollmentsController : Controller
    {
        // GET: EnrollementsController
        public ActionResult Index()
        {
            return View();
        }

        // GET: EnrollementsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: EnrollementsController/Create
        public ActionResult Create()
        {
            return View();
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]

      

    }
}
