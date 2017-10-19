using Microsoft.AspNet.Identity.Owin;
using PayPal.Api;
using PayPalintegration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace PayPalintegration.Controllers
{
    public class SingleController : Controller
    {
        private ApplicationDbContext _dbContext => HttpContext.GetOwinContext().Get<ApplicationDbContext>();

        public ActionResult Index()
        {
            var model = GetNextTourInfo();

            return View(model);
        }

        [HttpPost]
        public ActionResult Index(IndexVm model)
        {
            if (ModelState.IsValid)
            {
                Ticket tk = new Ticket();
                // Fetch the tour info from the server and NOT from the POST data.
                // Otherwise users could manipulate the data
                var tourInfo = GetNextTourInfo();
                var getinfo = (from e in _dbContext.Tickets where e.Email == model.Email select e).FirstOrDefault();
                if (getinfo == null)

                {


                    // Create a Ticket object to store info about the purchaser




                    tk.FirstName = model.FirstName;
                    tk.LastName = model.LastName;
                    tk.Email = model.Email;
                    tk.TourDate = tourInfo.TourDate;

                 
                    _dbContext.Tickets.Add(tk);
                    _dbContext.SaveChanges();
                    Session["username"] = model.Email;
                }

                if (getinfo!=null)
                {
                    Session["username"] = getinfo.Email;
                }

                // Get PayPal API Context using configuration from web.config
                var apiContext = GetApiContext();

                // Create a new payment object
                var payment = new Payment
                {
                    experience_profile_id = "XP-HMV5-V9ES-8QXN-2F33", // Created in the WebExperienceProfilesController. This one is for DigitalGoods.
                    intent = "sale",
                    payer = new Payer
                    {
                        payment_method = "paypal"
                    },
                    transactions = new List<Transaction>
                    {
                        new Transaction
                        {
                            description = $"Brewery Tour (Single Payment) for {tourInfo.TourDate:dddd, dd MMMM yyyy}",
                            amount = new Amount
                            {
                                currency = "USD",
                                total = (tourInfo.Price/100M).ToString(), // PayPal expects string amounts, eg. "20.00"
                            },
                            item_list = new ItemList()
                            {
                                items = new List<Item>()
                                {
                                    new Item()
                                    {
                                        description = $"Brewery Tour (Single Payment) for {tourInfo.TourDate:dddd, dd MMMM yyyy}",
                                        currency = "USD",
                                        quantity = "1",
                                        price = (tourInfo.Price/100M).ToString(), // PayPal expects string amounts, eg. "20.00"                                        
                                    }
                                }
                            }
                        }
                    },
                    redirect_urls = new RedirectUrls
                    {
                        return_url = Url.Action("Return", "Single", null, Request.Url.Scheme),
                        cancel_url = Url.Action("Cancel", "Single", null, Request.Url.Scheme)
                    }
                };

                // Send the payment to PayPal
                var createdPayment = payment.Create(apiContext);
                if (getinfo==null)
                {
                    tk.PayPalReference = createdPayment.id;
                    _dbContext.SaveChanges();

                }
                if (getinfo!=null)
                {
                    getinfo.PayPalReference = createdPayment.id;
                    _dbContext.SaveChanges();


                }
                // Save a reference to the paypal payment

                // Find the Approval URL to send our user to
                var approvalUrl =
                    createdPayment.links.FirstOrDefault(
                        x => x.rel.Equals("approval_url", StringComparison.OrdinalIgnoreCase));

                // Send the user to PayPal to approve the payment
                return Redirect(approvalUrl.href);
            }

            return View(model);
        }

        public ActionResult Return(string payerId, string paymentId)
        {
            // Fetch the existing ticket
            var ticket = _dbContext.Tickets.FirstOrDefault(x => x.PayPalReference == paymentId);

            // Get PayPal API Context using configuration from web.config
            var apiContext = GetApiContext();

            // Set the payer for the payment
            var paymentExecution = new PaymentExecution()
            {
                payer_id = payerId
            };

            // Identify the payment to execute
            var payment = new Payment()
            {
                id = paymentId
            };

            // Execute the Payment
            var executedPayment = payment.Execute(apiContext, paymentExecution);
            SendMail();
            return RedirectToAction("Thankyou");
        }

        public ActionResult Cancel()
        {
            
            return View();
        }

        public ActionResult ThankYou()
        {
            return View();
        }

        private IndexVm GetNextTourInfo()
        {
            return new IndexVm()
            {
                // Always set tour for tomorrow
                TourDate = DateTime.Today.AddDays(1),
                // Represent price in cents to avoid rounding errors
                Price = 2000
            };
        }

        private APIContext GetApiContext()
        {
            // Authenticate with PayPal
            var config = ConfigManager.Instance.GetProperties();
            var accessToken = new OAuthTokenCredential(config).GetAccessToken();
            var apiContext = new APIContext(accessToken);
            return apiContext;
        }
        public int getUserId(string usr)
        {

            var usnm = (from e in _dbContext.Tickets where e.Email == usr select e.Id).FirstOrDefault();
            return usnm;


        }

        public bool SendMail()
        {
            if (Session["username"] != null)
            {

                int tokenid = getUserId(Session["username"].ToString());
                var getinfo = (from e in _dbContext.Tickets where e.Id == tokenid select e).FirstOrDefault();
                string smtpAddress = "smtp.gmail.com";
                int portNumber = 587;
                bool enableSSL = true;
                bool result = false;

                string emailFrom = "crowdfundservice@gmail.com";
                string password = "M@ybenot0123456";
                string emailTo = getinfo.Email;
                string subject = "Password Reset";
                string body = "<b>Hello User</b>,<br>we have recieved a order we try to send your order soon as soon possible";

                using (MailMessage mail = new MailMessage())
                {
                    mail.From = new MailAddress(emailFrom);
                    mail.To.Add(emailTo);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = true;


                    using (SmtpClient smtp = new SmtpClient(smtpAddress, portNumber))
                    {
                        smtp.Credentials = new System.Net.NetworkCredential(emailFrom, password);
                        smtp.EnableSsl = enableSSL;
                        smtp.Send(mail);

                        result = true;
                    }
                }
                return result;
            }
            else
            {
                bool Err = false;
                return Err;
            }
        }
    }
}