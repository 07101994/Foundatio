﻿using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using Foundatio.Tests.Utility;
using Foundatio.Messaging;
using Xunit;

namespace Foundatio.Tests.Messaging {
    public class InMemoryMessagingTests {
        private IMessageBus _messageBus;

        protected virtual IMessageBus GetMessageBus() {
            if (_messageBus == null)
                _messageBus = new InMemoryMessageBus();

            return _messageBus;
        }

        [Fact]
        public void CanSendMessage() {
            var resetEvent = new AutoResetEvent(false);
            var messageBus = new InMemoryMessageBus();
            messageBus.Subscribe<SimpleMessageA>(msg => {
                Assert.Equal("Hello", msg.Data);
                resetEvent.Set();
            });
            messageBus.Publish(new SimpleMessageA {
                Data = "Hello"
            });

            bool success = resetEvent.WaitOne(100);
            Assert.True(success, "Failed to receive message.");
        }

        [Fact]
        public void CanSendDelayedMessage() {
            var resetEvent = new AutoResetEvent(false);
            var messageBus = new InMemoryMessageBus();
            messageBus.Subscribe<SimpleMessageA>(msg => {
                Assert.Equal("Hello", msg.Data);
                resetEvent.Set();
            });

            var sw = new Stopwatch();
            messageBus.Publish(new SimpleMessageA {
                Data = "Hello"
            }, TimeSpan.FromMilliseconds(100));

            sw.Start();
            bool success = resetEvent.WaitOne(1200);
            sw.Stop();

            Assert.True(success, "Failed to receive message.");
            Assert.True(sw.Elapsed > TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public void CanSendMessageToMultipleSubscribers() {
            var latch = new CountDownLatch(3);
            var messageBus = new InMemoryMessageBus();
            messageBus.Subscribe<SimpleMessageA>(msg => {
                Assert.Equal("Hello", msg.Data);
                latch.Signal();
            });
            messageBus.Subscribe<SimpleMessageA>(msg => {
                Assert.Equal("Hello", msg.Data);
                latch.Signal();
            });
            messageBus.Subscribe<SimpleMessageA>(msg => {
                Assert.Equal("Hello", msg.Data);
                latch.Signal();
            });
            messageBus.Publish(new SimpleMessageA {
                Data = "Hello"
            });

            bool success = latch.Wait(100);
            Assert.True(success, "Failed to receive all messages.");
        }

        [Fact]
        public void CanTolerateSubscriberFailure() {
            var latch = new CountDownLatch(2);
            var messageBus = new InMemoryMessageBus();
            messageBus.Subscribe<SimpleMessageA>(msg => {
                throw new ApplicationException();
            });
            messageBus.Subscribe<SimpleMessageA>(msg => {
                Assert.Equal("Hello", msg.Data);
                latch.Signal();
            });
            messageBus.Subscribe<SimpleMessageA>(msg => {
                Assert.Equal("Hello", msg.Data);
                latch.Signal();
            });
            messageBus.Publish(new SimpleMessageA {
                Data = "Hello"
            });

            bool success = latch.Wait(900);
            Assert.True(success, "Failed to receive all messages.");
        }

        [Fact]
        public void WillOnlyReceiveSubscribedMessageType() {
            var resetEvent = new AutoResetEvent(false);
            var messageBus = new InMemoryMessageBus();
            messageBus.Subscribe<SimpleMessageB>(msg => {
                Assert.True(false, "Received wrong message type.");
            });
            messageBus.Subscribe<SimpleMessageA>(msg => {
                Assert.Equal("Hello", msg.Data);
                resetEvent.Set();
            });
            messageBus.Publish(new SimpleMessageA {
                Data = "Hello"
            });

            bool success = resetEvent.WaitOne(100);
            Assert.True(success, "Failed to receive message.");
        }

        [Fact]
        public void WillReceiveDerivedMessageTypes() {
            var latch = new CountDownLatch(2);
            var messageBus = new InMemoryMessageBus();
            messageBus.Subscribe<ISimpleMessage>(msg => {
                Assert.Equal("Hello", msg.Data);
                latch.Signal();
            });
            messageBus.Publish(new SimpleMessageA {
                Data = "Hello"
            });
            messageBus.Publish(new SimpleMessageB {
                Data = "Hello"
            });
            messageBus.Publish(new SimpleMessageC {
                Data = "Hello"
            });

            bool success = latch.Wait(100);
            Assert.True(success, "Failed to receive all messages.");
        }

        [Fact]
        public void CanSubscribeToAllMessageTypes() {
            var latch = new CountDownLatch(3);
            var messageBus = new InMemoryMessageBus();
            messageBus.Subscribe<object>(msg => {
                latch.Signal();
            });
            messageBus.Publish(new SimpleMessageA {
                Data = "Hello"
            });
            messageBus.Publish(new SimpleMessageB {
                Data = "Hello"
            });
            messageBus.Publish(new SimpleMessageC {
                Data = "Hello"
            });

            bool success = latch.Wait(100);
            Assert.True(success, "Failed to receive all messages.");
        }
    }
}
