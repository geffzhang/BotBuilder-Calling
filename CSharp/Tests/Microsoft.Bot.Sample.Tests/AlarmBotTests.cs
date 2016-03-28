﻿// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Bot Framework: http://botframework.com
// 
// Bot Builder SDK Github:
// https://github.com/Microsoft/BotBuilder
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Models;
using Microsoft.Bot.Sample.SimpleAlarmBot;

namespace Microsoft.Bot.Sample.Tests
{
    [TestClass]
    public sealed class AlarmBotTests
    {
        public static IntentRecommendation[] IntentsFor(Expression<Func<SimpleAlarmBot.SimpleAlarmDialog, Task>> expression)
        {
            var body = (MethodCallExpression)expression.Body;
            var attribute = body.Method.GetCustomAttribute<LuisIntent>();
            var name = attribute.intentName;
            var intent = new IntentRecommendation(name);
            return new[] { intent };
        }

        public static EntityRecommendation EntityFor(string type, string entity)
        {
            return new EntityRecommendation(type) { Entity = entity };
        }

        public static void SetupLuis(
            Mock<ILuisService> luis,
            Expression<Func<SimpleAlarmBot.SimpleAlarmDialog, Task>> expression,
            params EntityRecommendation[] entities
            )
        {
            luis
                .Setup(l => l.QueryAsync(It.IsAny<Uri>()))
                .ReturnsAsync(new LuisResult()
                {
                    Intents = IntentsFor(expression),
                    Entities = entities
                });
        }

        [TestMethod]
        public async Task AlarmDialogFlow()
        {
            var luis = new Mock<ILuisService>();

            // arrange
            var now = DateTime.UtcNow;
            var entityTitle = EntityFor(SimpleAlarmBot.SimpleAlarmDialog.Entity_Alarm_Title, "title");
            var entityDate = EntityFor(SimpleAlarmBot.SimpleAlarmDialog.Entity_Alarm_Start_Date, now.ToShortDateString());
            var entityTime = EntityFor(SimpleAlarmBot.SimpleAlarmDialog.Entity_Alarm_Start_Time, now.ToShortTimeString());
            SetupLuis(luis, a => a.SetAlarm(null, null), entityTitle, entityDate, entityTitle);

            Func<IDialog> MakeRoot = () => new SimpleAlarmBot.SimpleAlarmDialog(luis.Object);
            var toBot = new Message() { ConversationId = Guid.NewGuid().ToString() };

            // act
            var toUser = await Conversation.SendAsync(toBot, MakeRoot, default(CancellationToken), luis.Object);

            // assert
            luis.VerifyAll();
            Assert.IsTrue(toUser.Text.Contains("created"));


            // arrange
            SetupLuis(luis, a => a.FindAlarm(null, null), entityTitle);

            // act
            toUser = await Conversation.SendAsync(toBot, MakeRoot, default(CancellationToken), luis.Object);

            // assert
            luis.VerifyAll();
            Assert.IsTrue(toUser.Text.Contains("found"));


            // arrange
            SetupLuis(luis, a => a.AlarmSnooze(null, null), entityTitle);

            // act
            toUser = await Conversation.SendAsync(toBot, MakeRoot, default(CancellationToken), luis.Object);

            // assert
            luis.VerifyAll();
            Assert.IsTrue(toUser.Text.Contains("snoozed"));


            // arrange
            SetupLuis(luis, a => a.TurnOffAlarm(null, null), entityTitle);

            // act
            toUser = await Conversation.SendAsync(toBot, MakeRoot, default(CancellationToken), luis.Object);

            // assert
            luis.VerifyAll();
            Assert.IsTrue(toUser.Text.Contains("sure"));


            // arrange
            toBot.Text = "blah";

            // act
            toUser = await Conversation.SendAsync(toBot, MakeRoot, default(CancellationToken), luis.Object);

            // assert
            luis.VerifyAll();
            Assert.IsTrue(toUser.Text.Contains("sure"));


            // arrange
            toBot.Text = "yes";

            // act
            toUser = await Conversation.SendAsync(toBot, MakeRoot, default(CancellationToken), luis.Object);

            // assert
            luis.VerifyAll();
            Assert.IsTrue(toUser.Text.Contains("disabled"));


            // arrange
            SetupLuis(luis, a => a.DeleteAlarm(null, null), entityTitle);

            // act
            toUser = await Conversation.SendAsync(toBot, MakeRoot, default(CancellationToken), luis.Object);

            // assert
            luis.VerifyAll();
            Assert.IsTrue(toUser.Text.Contains("did not find"));
        }
    }
}
