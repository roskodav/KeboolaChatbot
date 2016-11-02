﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KeboolaChatbot;
using Microsoft.Bot.Connector;

namespace DatabaseModel
{
    public partial class Conversation
    {
        [Key]
        public int ConversationID { get; set; }

        public string Name { get; set; }
        public string FrameworkId { get; set; }
        public virtual List<Message> Messages { get; set; }
        public string Detail { get; set; }
        public string BaseUri { get; set; }
        public virtual Customer Customer { get; set; }


        public static async Task<Conversation> CreateOrUpdateAsync(IMessageActivity activity, DatabaseContext db)
        {
            //Find conversation
            var conversation = await db.Conversation.FindByActivityAsync(activity);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    Customer = new Customer(),
                    Name = activity.Conversation.Name,
                    FrameworkId = activity.Conversation.Id,
                    BaseUri = activity.ServiceUrl
                };

                conversation.Customer.BotChannel = new Channel()
                {
                    FrameworkId = activity.Recipient.Id,
                    Name = activity.Recipient.Name
                };
                conversation.Customer.UserChannel = new Channel()
                {
                    FrameworkId = activity.From.Id,
                    Name = activity.From.Name
                };

                db.Conversation.Add(conversation);
            }

            conversation.Customer.ConversationID = conversation.ConversationID;
            await db.SaveChangesAsync();
            return conversation;
        }

        public void AddMessage(IMessageActivity activity, bool messageFromUser)
        {
            var text = String.Empty;
            if (activity.Text != String.Empty) //Simple message
            {
                text = activity.Text;
            }
            else //Advanced message with buttons images etc...
            {
                if (activity.Attachments?.Count > 0 &&
                    activity.Attachments?[0].Content is HeroCard)
                {
                    var card
                        = (HeroCard) activity.Attachments[0].Content;
                    text = card.Text + "{Buttons}";
                }
                else
                {
                    text = "Unknown message type";
                }
            }

            var message = new Message()
            {
                Text = text,
                Date = DateTime.Now,
                SendedByUser = messageFromUser
            };

            if (Messages == null)
                Messages = new List<Message>();

            Messages.Add(message);
        }
    }

    public static class ConversationExt
    {
        public static async Task<Conversation> FindByActivityAsync(this DbSet<Conversation> sbSet, IMessageActivity activity)
        {
            return
                await sbSet
                    .FirstOrDefaultAsync(
                        a =>
                            (a.FrameworkId == activity.Conversation.Id)
                            &&
                            a.BaseUri == activity.ServiceUrl
                    //Need check service url too, ConversationID is unique only for serviceUrl 
                    );
        }
    }
}