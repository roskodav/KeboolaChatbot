﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.Description;
using Keboola.Bot.Service;

namespace Keboola.Bot.Controllers
{
    public class StateModel
    {
        /// <summary>
        /// User is active or inactive
        /// </summary>
        [Required]
        public bool Active { get; set; }

        /// <summary>
        /// Keboola user token
        /// </summary>
        [Required]
        public string Token { get; set; }
    }

    /// <summary>
    /// State controller manage acces from keboola backend to chatbota backend.
    /// </summary>
    public class StateController : ApiController
    {
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private IDatabaseContext db = new DatabaseContext();
        private DatabaseService service;

        public StateController()
        {
            TimeSpan tokenExpirationDays = new TimeSpan(int.Parse(WebConfigurationManager.AppSettings["TokenExpirationDays"]), 0, 0, 0);
            DatabaseService.TokenExpiration = tokenExpirationDays;
            service = new DatabaseService(db);
        }

        public StateController(IDatabaseContext db, int tokenExpirationDays = 30)
        {
            this.db = db;
            service = new DatabaseService(db);
            DatabaseService.TokenExpiration = new TimeSpan(tokenExpirationDays, 0, 0, 0); ;

        }

        // POST api/state
        [ResponseType(typeof(void))]

        public async Task<IHttpActionResult> Post([FromBody] StateModel state)
        {
            //Validate input
            if (!ModelState.IsValid)
                return StatusCode(HttpStatusCode.BadRequest);
            try
            {
                //If token exist
                if (await service.TokenExistAsync(state.Token))
                    return StatusCode(HttpStatusCode.Conflict);

                //Add user
                var keboolaUser = await service.AddUserAndToken(state);

                if (state.Active)
                    await SendActivatedMessage(keboolaUser);

                return StatusCode(HttpStatusCode.OK);
            }
            catch (DbEntityValidationException ex)
            {
                logger.Error(ex);
                return StatusCode(HttpStatusCode.BadRequest);
            }
        }

        //PUT api/state/654dfs6sd54f
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> Put(string id, [FromBody] StateModel state)
        {
            //Validate input
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            if (String.IsNullOrEmpty(id))
                return BadRequest();
            try
            {
                //Find exist record
                var keboolaUser = await service.GetKeboolaUserByTokenAsync(id);

                //Ignor expired tokens or non-exist
                if (keboolaUser == null || DateTime.Now > keboolaUser.Token.Expiration)
                    return StatusCode(HttpStatusCode.NotFound);
                keboolaUser.Active = state.Active;
                await db.SaveChangesAsync();

                if (state.Active)
                    await SendActivatedMessage(keboolaUser);

                return StatusCode(HttpStatusCode.OK);
            }
            catch (DbEntityValidationException ex)
            {
                logger.Error(ex);
                return StatusCode(HttpStatusCode.BadRequest);
            }
        }

        private async Task SendActivatedMessage(KeboolaUser keboolaUser)
        {
            try
            {
                var conversation = await service.GetConversationAsync(keboolaUser);
                conversation.SendMessage("Your account is activated");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}