using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Sockets;
using Moq;

namespace NNTP_Project.Tests
{
    [TestClass()]
    public class MainWindowTests
    {
        [TestMethod()]
        public void MainWindowTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void ListNewsGroupsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void LogInTest()
        {
            var mainWindow = new MainWindow();

            var mockTcpClient = new Mock<TcpClient>();
            var mockReader = new Mock<StreamReader>();
            var mockWriter = new Mock<StreamWriter>();

            mainWindow.client = mockTcpClient.Object;
            mainWindow.reader = mockReader.Object;
            mainWindow.writer = mockWriter.Object;

            mockReader.SetupSequence(reader => reader.ReadLine())
                .Returns("381 Pleaser enter your password.")
                .Returns("281 Authenticaion accepted.");

            try
            {
                mainWindow.LogIn();
            }
            catch (Exception ex)
            {
                Assert.Fail($"An unexpected exception was thrown: {ex}");
            }
        }
    }
}