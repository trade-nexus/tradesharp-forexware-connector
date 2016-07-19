using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using TradeHub.Common.Core.DomainModels;
using TradeHub.Common.Core.DomainModels.OrderDomain;
using TradeHub.Common.Core.ValueObjects.MarketData;
using TradeSharp.OrderExecutionProvider.Forexware.Provider;
using Constants = TradeHub.Common.Core.Constants;

namespace TradeSharp.OrderExecutionProvider.Forexware.Tests.Integration
{
    [TestFixture]
    class ProviderTestCases
    {
        private ForexwareOrderExecutionProvider _executionProvider;

        [SetUp]
        public void SetUp()
        {
            _executionProvider = new ForexwareOrderExecutionProvider();
        }

        [Test]
        [Category("Integration")]
        public void ConnectOrderExecutionProviderTestCase()
        {
            bool isConnected = false;
            var manualLogonEvent = new ManualResetEvent(false);

            _executionProvider.LogonArrived +=
                    delegate (string obj)
                    {
                        isConnected = true;
                        manualLogonEvent.Set();
                    };

            _executionProvider.Start();
            manualLogonEvent.WaitOne(30000, false);

            Assert.AreEqual(true, isConnected);
        }

        [Test]
        [Category("Integration")]
        public void DisconnectOrderExecutionProviderTestCase()
        {
            bool isConnected = false;
            var manualLogonEvent = new ManualResetEvent(false);
            _executionProvider.LogonArrived +=
                    delegate (string obj)
                    {
                        isConnected = true;
                        _executionProvider.Stop();
                        manualLogonEvent.Set();
                    };

            bool isDisconnected = false;
            var manualLogoutEvent = new ManualResetEvent(false);
            _executionProvider.LogoutArrived +=
                    delegate (string obj)
                    {
                        isDisconnected = true;
                        manualLogoutEvent.Set();
                    };

            _executionProvider.Start();
            manualLogonEvent.WaitOne(30000, false);
            manualLogoutEvent.WaitOne(30000, false);

            Assert.AreEqual(true, isConnected, "Connected");
            Assert.AreEqual(true, isDisconnected, "Disconnected");
        }

        [Test]
        [Category("Integration")]
        public void MarketOrder_OrderExecutionProviderTestCase()
        {
            MarketOrder marketOrder = new MarketOrder(Constants.OrderExecutionProvider.Forexware)
            {
                OrderID = "5000",
                Security = new Security { Symbol = "EUR/USD" },
                OrderSide = Constants.OrderSide.BUY,
                OrderSize = 1000,
                OrderTif = Constants.OrderTif.GTC
            };

            bool isConnected = false;
            bool isDisconnected = false;
            bool newArrived = false;
            bool executionArrived = false;

            var manualLogoutEvent = new ManualResetEvent(false);
            var manualLogonEvent = new ManualResetEvent(false);
            var manualNewEvent = new ManualResetEvent(false);
            var manualExecutionEvent = new ManualResetEvent(false);

            _executionProvider.LogonArrived +=
                    delegate (string obj)
                    {
                        isConnected = true;
                        _executionProvider.SendMarketOrder(marketOrder);
                        manualLogonEvent.Set();
                    };

            _executionProvider.NewArrived += delegate (Order order)
            {
                newArrived = true;
                manualNewEvent.Set();
            };

            _executionProvider.ExecutionArrived += delegate (Execution order)
            {
                if (order.Fill.LeavesQuantity.Equals(0))
                {
                    executionArrived = true;
                    _executionProvider.Stop();
                    manualExecutionEvent.Set();
                }
            };

            _executionProvider.LogoutArrived +=
                    delegate (string obj)
                    {
                        isDisconnected = true;
                        manualLogoutEvent.Set();
                    };

            _executionProvider.Start();

            manualLogonEvent.WaitOne(30000, false);
            manualNewEvent.WaitOne(30000, false);
            manualExecutionEvent.WaitOne(30000, false);
            manualLogoutEvent.WaitOne(30000, false);

            Assert.AreEqual(true, isConnected, "Connected");
            Assert.AreEqual(true, newArrived, "New Arrived");
            Assert.AreEqual(true, executionArrived, "Execution Arrived");
            Assert.AreEqual(true, isDisconnected, "Disconnected");
        }
    }
}
