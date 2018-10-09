﻿using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using Orckestra.Composer.Configuration;
using Orckestra.Composer.MyAccount.Parameters;
using Orckestra.Composer.MyAccount.Providers;
using Orckestra.Composer.MyAccount.Requests;
using Orckestra.Composer.MyAccount.Responses;
using Orckestra.Composer.MyAccount.Services;
using Orckestra.Composer.MyAccount.ViewModels;
using Orckestra.Composer.Parameters;
using Orckestra.Composer.Providers;
using Orckestra.Composer.Services;
using Orckestra.Composer.Utils;
using Orckestra.Composer.WebAPIFilters;
using LoginViewModel = Orckestra.Composer.MyAccount.Requests.LoginViewModel;

namespace Orckestra.Composer.MyAccount.Api
{
    /// <summary>
    /// WebApi Controller for Action to perform on User Accounts
    /// User accounts can be Customers, Administrors, etc.
    /// </summary>
    /// <remarks>
    /// This controller contains non-async methods. Please leave them. Some methods depend on HttpContext.Current
    /// being set, and making the method async loses this context.
    /// </remarks>
    [ValidateLanguage]
    [JQueryOnlyFilter]
    public class MembershipController : ApiController
    {
        private readonly ComposerCookieAccesserConfigurationElement _cookieConfig;

        protected IMyAccountUrlProvider MyAccountUrlProvider { get; }
        protected IMembershipViewService MembershipViewService { get; }
        protected IComposerContext ComposerContext { get; }
        internal IFormsAuthenticationProxy FormsAuthentication { private get; set; }

        public MembershipController(
            IMyAccountUrlProvider myAccountUrlProvider,
            IMembershipViewService membershipViewService,
            IComposerContext composerContext)
        {
            if (myAccountUrlProvider == null) { throw new ArgumentNullException(nameof(myAccountUrlProvider)); }
            if (membershipViewService == null) { throw new ArgumentNullException(nameof(membershipViewService)); }
            if (composerContext == null) { throw new ArgumentNullException(nameof(composerContext)); }

            MyAccountUrlProvider = myAccountUrlProvider;
            MembershipViewService = membershipViewService;
            ComposerContext = composerContext;

            //Configurations
            //It's ok to omit this config, we simply fallback to the default values
            var conf = ConfigurationManager.GetSection(ComposerConfigurationSection.ConfigurationName) as ComposerConfigurationSection;
            var confComposer = conf ?? new ComposerConfigurationSection();
            var confCookie = confComposer.ComposerCookieAccesser ?? new ComposerCookieAccesserConfigurationElement();

            FormsAuthentication = new StaticFormsAuthenticationProxy();

            _cookieConfig = confCookie;
        }

        /// <summary>
        /// Signs the user in.
        /// </summary>
        /// <param name="loginRequest"></param>
        /// <returns></returns>
        /// <remarks>This method cannot be async.</remarks>
        [AllowAnonymous]
        [HttpPost]
        [ActionName("login")]
        [ValidateModelState]
        public virtual IHttpActionResult SignIn(LoginViewModel loginRequest)
        {
            if (loginRequest == null) { return BadRequest("Request body cannot be null"); }

            var returnUrl = loginRequest.ReturnUrl;

            if (string.IsNullOrWhiteSpace(returnUrl) || !UrlFormatter.IsReturnUrlValid(RequestUtils.GetBaseUrl(Request).ToString(), returnUrl))
            {
                returnUrl = MyAccountUrlProvider.GetMyAccountUrl(new GetMyAccountUrlParam
                {
                    CultureInfo = ComposerContext.CultureInfo
                });
            }

            var loginParam = new LoginParam
            {
                Password = loginRequest.Password,
                Username = loginRequest.Username,
                Scope = ComposerContext.Scope,
                CultureInfo = ComposerContext.CultureInfo,
                ReturnUrl = returnUrl,
                GuestCustomerId = ComposerContext.CustomerId,
            };

            var loginViewModel = MembershipViewService.LoginAsync(loginParam).Result;

            if (!loginViewModel.IsSuccess) { return Ok(loginViewModel); }

            ComposerContext.CustomerId = loginViewModel.CustomerId;
            ComposerContext.IsGuest = false;

            if (loginRequest.IsRememberMe)
            {
                var ticket = new FormsAuthenticationTicket(loginViewModel.Username, loginRequest.IsRememberMe, _cookieConfig.Timeout);
                var encrypted = FormsAuthentication.Encrypt(ticket);

                var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted)
                {
                    Expires = DateTime.Now.AddMinutes(_cookieConfig.Timeout),
                    HttpOnly = true,
                    Secure = _cookieConfig.RequireSsl
                };

                HttpContext.Current.Response.Cookies.Add(cookie);
            }
            else
            {
                FormsAuthentication.SetAuthCookie(loginViewModel.Username, true);
            }

            return Ok(loginViewModel);
        }

        /// <summary>
        /// Post to logout the user
        /// Removing and Invalidating the FormsAuthentication Cookie and the Composer Cookie
        /// </summary>
        /// <param name="logoutRequest">The WebApi Request arguments <see cref="LogoutRequest"/></param>
        /// <returns>
        ///    The updated ViewModel <see cref="LogoutResponse"/> for rerendering or updating
        /// </returns>
        [HttpPost]
        [ActionName("logout")]
        [ValidateModelState]
        public virtual IHttpActionResult Logout(LogoutRequest logoutRequest)
        {
            if (logoutRequest == null) { throw new ArgumentNullException(nameof(logoutRequest)); }

            var response = new LogoutResponse();

            if (!logoutRequest.PreserveCustomerInfo)
            {
                InvalidateCustomerCookie();
            }

            FormsAuthentication.SignOut();

            response.ReturnUrl = logoutRequest.ReturnUrl;
            if (string.IsNullOrWhiteSpace(response.ReturnUrl) || !UrlFormatter.IsReturnUrlValid(RequestUtils.GetBaseUrl(Request).ToString(), response.ReturnUrl))
            {
                response.ReturnUrl = MyAccountUrlProvider.GetLoginUrl(new GetMyAccountUrlParam
                {
                    CultureInfo = ComposerContext.CultureInfo
                });
            }

            return Ok(response);
        }

        /// <summary>
        /// Remove information relative to the Customer himself
        /// </summary>
        protected virtual void InvalidateCustomerCookie()
        {
            ComposerContext.CustomerId = Guid.Empty;
            ComposerContext.IsGuest = true;
        }

        /// <summary>
        /// Create the Account based on the Request
        /// Use System configuration in Overture for password and account validation
        /// When Overture is configured to create account in Active status, On Success, the user is automatically logged.
        /// When Overture is configured to create account in a RequiresApproval status, On Success, the user is not logged.
        /// On Failure, the Status can be used to identify the failure causes.
        /// </summary>
        /// <param name="registerRequest"></param>
        /// <returns></returns>
        /// <remarks>This method should not be async.</remarks>
        [AllowAnonymous]
        [HttpPost]
        [ActionName("register")]
        [ValidateModelState]
        public virtual IHttpActionResult Register(RegisterRequest registerRequest)
        {
            //todo:  Captcha for bots or not?!
            if (registerRequest == null) { return BadRequest("No body found in the request"); }

            var returnUrl = registerRequest.ReturnUrl;

            if (string.IsNullOrWhiteSpace(returnUrl) || !UrlFormatter.IsReturnUrlValid(RequestUtils.GetBaseUrl(Request).ToString(), returnUrl))
            {
                returnUrl = MyAccountUrlProvider.GetMyAccountUrl(new GetMyAccountUrlParam
                {
                    CultureInfo = ComposerContext.CultureInfo
                });
            }

            var registerParam = new CreateUserParam
            {
                Username = registerRequest.Username,
                Password = registerRequest.Password,
                Email = registerRequest.Email,
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                PasswordQuestion = registerRequest.PasswordQuestion,
                PasswordAnswer = registerRequest.PasswordAnswer,
                ReturnUrl = returnUrl,
                Scope = ComposerContext.Scope,
                CultureInfo = ComposerContext.CultureInfo,
                GuestCustomerId = ComposerContext.CustomerId
            };

            var createAccountViewModel = MembershipViewService.RegisterAsync(registerParam).Result;

            if (!createAccountViewModel.IsSuccess) { return Ok(createAccountViewModel); }

            var loginParam = new LoginParam
            {
                CultureInfo = ComposerContext.CultureInfo,
                Scope = ComposerContext.Scope,
                GuestCustomerId = ComposerContext.CustomerId,
                Username = registerRequest.Email,
                Password = registerRequest.Password
            };

            var loginViewModel = MembershipViewService.LoginAsync(loginParam).Result;

            ComposerContext.IsGuest = false;
            ComposerContext.CustomerId = createAccountViewModel.CustomerId;
            FormsAuthentication.SetAuthCookie(createAccountViewModel.Username, true);

            return Ok(createAccountViewModel);
        }

        /// <summary>
        /// Change Password for the currently logged user.
        /// </summary>
        /// <param name="changePasswordRequest">WebApi Request arguments <see cref="ChangePasswordRequest"/></param>
        /// <returns>The updated <see cref="ChangePasswordViewModel" /> for rerendering or redirecting</returns>
        [Authorize]
        [HttpPost]
        [ActionName("changepassword")]
        [ValidateModelState]
        public virtual async Task<IHttpActionResult> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest)
        {
            if (changePasswordRequest == null) { return BadRequest("No body found in the request"); }
            if (!ComposerContext.IsAuthenticated) { return Unauthorized(); }

            var returnUrl = changePasswordRequest.ReturnUrl;

            if (!UrlFormatter.IsReturnUrlValid(RequestUtils.GetBaseUrl(Request).ToString(), returnUrl))
            {
                returnUrl = null;
            }

            var changePasswordViewModel = await MembershipViewService.ChangePasswordAsync(new ChangePasswordParam
            {
                CustomerId = ComposerContext.CustomerId,
                NewPassword = changePasswordRequest.NewPassword,
                OldPassword = changePasswordRequest.OldPassword,
                Scope = ComposerContext.Scope,
                CultureInfo = ComposerContext.CultureInfo,
                ReturnUrl = returnUrl
            });

            return Ok(changePasswordViewModel);
        }

        /// <summary>
        /// Sets the new password for the user idenfied by the secure Ticket.
        /// </summary>
        /// <param name="resetPasswordRequest"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [ActionName("resetpassword")]
        [ValidateModelState]
        public virtual async Task<IHttpActionResult> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest)
        {
            if (resetPasswordRequest == null) { return BadRequest("No body found in the request"); }

            var returnUrl = resetPasswordRequest.ReturnUrl;

            if (string.IsNullOrWhiteSpace(returnUrl) || !UrlFormatter.IsReturnUrlValid(RequestUtils.GetBaseUrl(Request).ToString(), returnUrl))
            {
                returnUrl = MyAccountUrlProvider.GetLoginUrl(new GetMyAccountUrlParam
                {
                    CultureInfo = ComposerContext.CultureInfo
                });
            }

            var ticket = HttpUtility.UrlDecode(resetPasswordRequest.Ticket);

            var resetPasswordViewModel = await MembershipViewService.ResetPasswordAsync(new ResetPasswordParam
            {
                Ticket = ticket,
                NewPassword = resetPasswordRequest.NewPassword,
                Scope = ComposerContext.Scope,
                CultureInfo = ComposerContext.CultureInfo,
                ReturnUrl = returnUrl
            });

            return Ok(resetPasswordViewModel);
        }

        /// <summary>
        /// Send instructions to the user on how to reset it's password.
        /// </summary>
        /// <param name="forgotPasswordRequest"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [ActionName("forgotpassword")]
        [ValidateModelState]
        public virtual async Task<IHttpActionResult> ForgotPasswordAsync(ForgotPasswordRequest forgotPasswordRequest)
        {
            if (forgotPasswordRequest == null) { return BadRequest("No body found in the request"); }

            var forgotPasswordViewModel = await MembershipViewService.ForgotPasswordAsync(new ForgotPasswordParam
            {
                Email = forgotPasswordRequest.Email,
                Scope = ComposerContext.Scope,
                CultureInfo = ComposerContext.CultureInfo
            });

            return Ok(forgotPasswordViewModel);
        }

        /// <summary>
        /// Get the mini sign-in viewmodel in the header
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        [ActionName("signin")]
        public virtual async Task<IHttpActionResult> SignIn()
        {
            var getSignInHeaderParam = new GetSignInHeaderParam
            {
                CustomerId = ComposerContext.CustomerId,
                CultureInfo = ComposerContext.CultureInfo,
                Scope = ComposerContext.Scope,
                IsAuthenticated = ComposerContext.IsAuthenticated,
                EncryptedCustomerId = ComposerContext.GetEncryptedCustomerId()
            };

            var signInHeaderViewModel = await MembershipViewService.GetSignInHeaderModel(getSignInHeaderParam);

            return Ok(signInHeaderViewModel);
        }

        /// <summary>
        /// Return true if the user is authenticated
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        [ActionName("isAuthenticated")]
        public virtual IHttpActionResult IsAuthenticated()
        {
            var vm = new IsAuthenticatedViewModel
            {
                IsAuthenticated = ComposerContext.IsAuthenticated
            };

            return Ok(vm);
        }
    }
}