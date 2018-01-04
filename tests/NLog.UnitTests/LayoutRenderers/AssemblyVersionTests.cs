// 
// Copyright (c) 2004-2017 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
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

namespace NLog.UnitTests.LayoutRenderers
{
    using System;
    using System.Reflection;
    using NLog.LayoutRenderers;
    using Xunit;
	using Xunit.Abstractions;

    public class AssemblyVersionTests : NLogTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public AssemblyVersionTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void EntryAssemblyVersionTest()
        {
            var assembly = Assembly.GetEntryAssembly();
            var assemblyVersion = assembly == null ?
                $"Could not find value for entry assembly and version type {nameof(AssemblyVersionType.Assembly)}"
                : assembly.GetName().Version.ToString();
            AssertLayoutRendererOutput("${assembly-version}", assemblyVersion);
        }

        [Fact]
        public void AssemblyNameVersionTest()
        {
            AssertLayoutRendererOutput("${assembly-version:NLogAutoLoadExtension}", "2.0.0.0");
        }

        [Fact]
        public void AssemblyNameVersionTypeTest()
        {
            AssertLayoutRendererOutput("${assembly-version:name=NLogAutoLoadExtension:type=assembly}", "2.0.0.0");
            AssertLayoutRendererOutput("${assembly-version:name=NLogAutoLoadExtension:type=file}", "2.0.0.0");
            AssertLayoutRendererOutput("${assembly-version:name=NLogAutoLoadExtension:type=informational}", "1.0.0");
        }

#if !NETSTANDARD
        private const string AssemblyVersionTest = "1.1.1.1";
        private const string AssemblyFileVersionTest = "1.1.1.2";
        private const string AssemblyInformationalVersionTest = "Version 1";

        [Theory]
        [InlineData(AssemblyVersionType.Assembly, AssemblyVersionTest)]
        [InlineData(AssemblyVersionType.File, AssemblyFileVersionTest)]
        [InlineData(AssemblyVersionType.Informational, AssemblyInformationalVersionTest)]
        public void AssemblyVersionTypeTest(AssemblyVersionType type, string expected)
        {
            LogManager.Configuration = CreateConfigurationFromString(@"
            <nlog>
                <targets><target name='debug' type='Debug' layout='${message:withException=true}|${assembly-version:type=" + type.ToString().ToLower() + @"}' /></targets>
                <rules><logger name='*' minlevel='Debug' writeTo='debug' /></rules>
            </nlog>");
            var logger = LogManager.GetLogger("SomeLogger");
            var compiledAssembly = GenerateTestAssembly();
            var testLoggerType = compiledAssembly.GetType("LogTester.LoggerTest");
            var logMethod = testLoggerType.GetMethod("TestLog");
            var testLoggerInstance = Activator.CreateInstance(testLoggerType);

            logMethod.Invoke(testLoggerInstance, new object[] { logger, compiledAssembly });

            var lastMessage = GetDebugLastMessage("debug");
            var messageParts = lastMessage.Split('|');
            var logMessage = messageParts[0];
            var logVersion = messageParts[1];
            if (logMessage.StartsWith("Skip:"))
            {
                _testOutputHelper.WriteLine(logMessage);
            }
            else
            {
                Assert.StartsWith("Pass:", logMessage);
                Assert.Equal(expected, logVersion);
            }
        }

        private static Assembly GenerateTestAssembly()
        {
            const string code = @"
                using System;
                using System.Reflection;

                [assembly: AssemblyVersion(""" + AssemblyVersionTest + @""")]
                [assembly: AssemblyFileVersion(""" + AssemblyFileVersionTest + @""")]
                [assembly: AssemblyInformationalVersion(""" + AssemblyInformationalVersionTest + @""")]

                namespace LogTester
                {
                    public class LoggerTest
                    {
                        public void TestLog(NLog.Logger logger, Assembly assembly)
                        {
                            if (System.Reflection.Assembly.GetEntryAssembly() == null)
                            {
                                // In some unit testing scenarios we cannot find the entry assembly
                                // So we attempt to force this to be set, which can also still fail
                                // This is not expected to be necessary in Visual Studio
                                // See https://github.com/Microsoft/vstest/issues/649
                                try
                                {
                                    SetEntryAssembly(assembly);
                                }
                                catch (InvalidOperationException ioex)
                                {
                                    logger.Debug(ioex, ""Skip: No entry assembly"");
                                    return;
                                }
                            }

                            logger.Debug(""Pass: Test fully executed"");
                        }

                        private static void SetEntryAssembly(Assembly assembly)
                        {
                            var manager = new AppDomainManager();

                            var domain = AppDomain.CurrentDomain;
                            if (domain == null)
                                throw new InvalidOperationException(""Current app domain is null"");

                            var entryAssemblyField = manager.GetType().GetField(""m_entryAssembly"", BindingFlags.Instance | BindingFlags.NonPublic);
                            if (entryAssemblyField == null)
                                throw new InvalidOperationException(""Unable to find field m_entryAssembly"");
                            entryAssemblyField.SetValue(manager, assembly);

                            var domainManagerField = domain.GetType().GetField(""_domainManager"", BindingFlags.Instance | BindingFlags.NonPublic);
                            if (domainManagerField == null)
                                throw new InvalidOperationException(""Unable to find field _domainManager"");
                            domainManagerField.SetValue(domain, manager);
                        }
                    }
                }";

            var provider = new Microsoft.CSharp.CSharpCodeProvider();
            var parameters = new System.CodeDom.Compiler.CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
                ReferencedAssemblies = {"NLog.dll"}
            };
            System.CodeDom.Compiler.CompilerResults results = provider.CompileAssemblyFromSource(parameters, code);
            var compiledAssembly = results.CompiledAssembly;
            return compiledAssembly;
        }
#endif
    }
}
