using System;
using System.Collections.Generic;
using System.Text;
using FolderSyncer.Core.Network.Protocol;
using Xunit;

namespace FolderSyncer.Core.Tests.Network.Protocol
{
    public class ProtocolDialogClientTests
    {
        [Fact]
        public void Constructor_NullStream_ShouldThrowException()
        {
            // Arrange Act Assert
            Assert.Throws<ArgumentNullException>(() => new ProtocolDialogClient(null));
        }
    }
}
