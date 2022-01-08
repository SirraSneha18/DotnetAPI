// 
// Copyright (c) 2004-2021 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

namespace NLog.UnitTests.Config
{
    using System;
    using NLog.Config;
    using NLog.Targets;
    using System.Collections.Generic;
    using Xunit;

    public class ConfigurationItemFactoryTests : NLogTestBase
    {
        [Fact]
        public void ConfigurationItemFactoryDefaultTest()
        {
            var itemFactory = new ConfigurationItemFactory();
            Assert.IsType<DebugTarget>(itemFactory.CreateInstance(typeof(DebugTarget)));
        }

        [Fact]
        public void ConfigurationItemFactorySimpleTest()
        {
            var cif = new ConfigurationItemFactory();
            cif.RegisterType(typeof(DebugTarget), string.Empty);
            var target = cif.Targets.CreateInstance("Debug") as DebugTarget;
            Assert.NotNull(target);
        }

        [Fact]
        public void ConfigurationItemFactoryUsesSuppliedDelegateToResolveObject()
        {
            var cif = new ConfigurationItemFactory();
            cif.RegisterType(typeof(DebugTarget), string.Empty);
            List<Type> resolvedTypes = new List<Type>();
            cif.CreateInstance = t => { resolvedTypes.Add(t); return Activator.CreateInstance(t); };
            Target target = cif.Targets.CreateInstance("Debug");
            Assert.NotNull(target);
            Assert.Single(resolvedTypes);
            Assert.Equal(typeof(DebugTarget), resolvedTypes[0]);
        }

#if !NETSTANDARD && !MONO

        [Fact]
        public void ExtendedLayoutRendererTest()
        {
            var layoutRenderers = ConfigurationItemFactory.Default.LayoutRenderers;

            AssertInstance(layoutRenderers, "appsetting", "AppSettingLayoutRenderer", "AppSettingLayoutRenderer2");
        }

        private static void AssertInstance<T1, T2>(INamedItemFactory<T1, T2> targets, string itemName, params string[] expectedTypeNames)
            where T1 : class
        {
            Assert.Contains(targets.CreateInstance(itemName).GetType().Name, expectedTypeNames);
        }
#endif

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("name", "name")]
        [InlineData("name-two", "nametwo")]
        [InlineData("name-two, my-assembly", "nametwo, my-assembly")]
        [InlineData(", my-assembly", ", my-assembly")] // border case
        [InlineData("name,", "name,")] // border case
        public void NormalizeNameTest(string input, string expected)
        {
            // Arrange

            // Act
            var result = LayoutRendererFactory.NormalizeName(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
