using NUnit.Framework;
using UnityEngine;

namespace FruitTree.Tests
{
    public sealed class BrandingTests
    {
        [Test]
        public void HomeLogoShipsAsWideTransparentArt()
        {
            var logo = Resources.Load<Texture2D>(HomeLogo.ResourcePath);
            Assert.IsNotNull(logo);
            Assert.Greater(logo.width, logo.height * 2);
        }
    }
}
