﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Tests;
using Microsoft.Bot.Connector;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Keboola.Bot.Tests
{
    [TestClass]
    public class RootDialogtest : DialogTestBase
    {
        private readonly Queue<IMessageActivity> responses = new Queue<IMessageActivity>();
        private IContainer container;
        private Func<IDialog<object>> MakeRoot;
        private IMessageActivity toBot;


        public async Task FromUser(string text)
        {
            Debug.WriteLine("User:" + text);
            toBot.Text = text;
            await GetResponses();
        }

        private async Task GetResponses()
        {
            var responsese = await GetResponse(null, MakeRoot, toBot);
            foreach (var messageActivity in responsese)
                responses.Enqueue(messageActivity);
        }

        public bool FromBot(string text)
        {
            var msg = responses.Dequeue();
            if (msg.Attachments.Count > 0)
            {
                var response = ((HeroCard) msg.Attachments[0].Content).Text;
                Debug.WriteLine("Bot:" + response + " (hero)");
                return response.ToLower().Contains(text.ToLower());
            }
            Debug.WriteLine("Bot:" + msg.Text);
            return msg.Text.ToLower().Contains(text.ToLower());
        }

        /*
        private void InitDialog(out Func<IDialog<object>> MakeRoot, out IContainer container)
        {
            var echoDialog = new RootDialog().BuildChain();
            toBot = MakeTestMessage();
            toBot.From.Id = Guid.NewGuid().ToString();
            toBot.Text = "ConversationUpdate";
            MakeRoot = () => echoDialog;
            new FiberTestBase.ResolveMoqAssembly(echoDialog);
            container = Build(Options.MockConnectorFactory | Options.ScopedQueue, echoDialog);

        }
        */

        private async Task<Queue<IMessageActivity>> GetResponse(IContainer container, Func<IDialog<object>> makeRoot,
            IMessageActivity toBot)
        {
            using (var scope = DialogModule.BeginLifetimeScope(container, toBot))
            {
                DialogModule_MakeRoot.Register(scope, makeRoot);
                //  var token = CancellationToken.None;
                using (new LocalizedScope(toBot.Locale))
                {
                    var task = scope.Resolve<IPostToBot>();
                    await task.PostAsync(toBot, default(CancellationToken));
                }
                // await Conversation.SendAsync(scope, toBot);
                return scope.Resolve<Queue<IMessageActivity>>();
            }
        }
    }
}