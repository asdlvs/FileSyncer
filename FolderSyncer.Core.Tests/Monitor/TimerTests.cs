using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using Timer = FolderSyncer.Core.Monitor.Timer;

namespace FolderSyncer.Core.Tests.Monitor
{
    public class TimerTests
    {
        [Fact]
        public void Constructor_WithNullConfiguration_ShouldThrowException()
        {
            // Arrange -> Act -> Assert
            Assert.Throws<ArgumentNullException>(() => new Timer(null));
        }

        [Fact]
        public void Constructor_WithCorrectArguments_ShouldProperlySetTimer()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(config => config["snapshot_interval"]).Returns("20");

            // Act
            var timer = new Timer(configuration.Object);
            int counter = 0;
            timer.Elapsed += (sender, args) => { counter++; };
            timer.Start();
            Thread.Sleep(120);
            timer.Stop();
            Thread.Sleep(200);

            // Assert
            Assert.InRange(counter, 5, 6);
        }

        [Fact]
        public void Constructor_WithEmptyTimeout_ShouldSetItAutomatically()
        {
            // Arrange
            var configuration = new Mock<IConfiguration>();
            configuration.Setup(config => config["snapshot_interval"]).Returns("");

            // Act
            var timer = new Timer(configuration.Object);

            // Assert
            Assert.Equal(5000, timer.Interval);
        }
    }
}
