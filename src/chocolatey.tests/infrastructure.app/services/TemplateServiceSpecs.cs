﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.tests.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.app.templates;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.services;
    using Moq;
    using NuGet.Common;
    using NUnit.Framework;
    using FluentAssertions;
    using LogLevel = tests.LogLevel;

    public class TemplateServiceSpecs
    {
        public abstract class TemplateServiceSpecsBase : TinySpec
        {
            protected TemplateService service;
            protected Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
            protected Mock<IXmlService> xmlService = new Mock<IXmlService>();
            protected Mock<ILogger> logger = new Mock<ILogger>();

            public override void Context()
            {
                fileSystem.ResetCalls();
                xmlService.ResetCalls();
                service = new TemplateService(fileSystem.Object, xmlService.Object, logger.Object);
            }
        }

        public class When_generate_noop_is_called : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                fileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                config.NewCommand.Name = "Bob";
            }

            public override void Because()
            {
                because = () => service.GenerateDryRun(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            public void Should_log_current_directory_if_no_outputdirectory()
            {
                because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0,"Would have generated a new package specification at c:\\chocolatey\\Bob");
            }

            [Fact]
            public void Should_log_output_directory_if_outputdirectory_is_specified()
            {
                config.OutputDirectory = "c:\\packages";

                because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0,"Would have generated a new package specification at c:\\packages\\Bob");
            }
        }

        public class When_generate_file_from_template_is_called : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private readonly TemplateValues templateValues = new TemplateValues();
            private readonly string template = "[[PackageName]]";
            private const string fileLocation = "c:\\packages\\bob.nuspec";
            private string fileContent;

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.WriteFile(It.Is((string fl) => fl == fileLocation), It.IsAny<string>(), Encoding.UTF8))
                    .Callback((string filePath, string fileContent, Encoding encoding) => this.fileContent = fileContent);

                templateValues.PackageName = "Bob";
            }

            public override void Because()
            {
                because = () => service.GenerateFileFromTemplate(config, templateValues, template, fileLocation, Encoding.UTF8);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            public void Should_write_file_withe_replaced_tokens()
            {
                because();

                var debugs = MockLogger.MessagesFor(LogLevel.Debug);
                debugs.Should().ContainSingle();
                debugs.Should().HaveElementAt(0,"Bob");
            }

            [Fact]
            public void Should_log_info_if_regular_output()
            {
                config.RegularOutput = true;

                because();

                var debugs = MockLogger.MessagesFor(LogLevel.Debug);
                debugs.Should().ContainSingle();
                debugs.Should().HaveElementAt(0,"Bob");

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0,string.Format(@"Generating template to a file{0} at 'c:\packages\bob.nuspec'", Environment.NewLine));
            }
        }

        public class When_generate_is_called_with_existing_directory : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private string verifiedDirectoryPath;

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                fileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns<string>(
                    x =>
                    {
                        verifiedDirectoryPath = x;
                        return true;
                    });

                config.NewCommand.Name = "Bob";
            }

            public override void Because()
            {
                because = () => service.Generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            public void Should_throw_exception()
            {
                bool errored = false;
                string errorMessage = string.Empty;

                try
                {
                    because();
                }
                catch (ApplicationException ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.Should().BeTrue();
                errorMessage.Should().Be(string.Format("The location for the template already exists. You can:{0} 1. Remove 'c:\\chocolatey\\Bob'{0} 2. Use --force{0} 3. Specify a different name", Environment.NewLine));
                verifiedDirectoryPath.Should().Be("c:\\chocolatey\\Bob");
            }

            [Fact]
            public void Should_throw_exception_even_with_outputdirectory()
            {
                config.OutputDirectory = "c:\\packages";

                bool errored = false;
                string errorMessage = string.Empty;

                try
                {
                    because();
                }
                catch (ApplicationException ex)
                {
                    errored = true;
                    errorMessage = ex.Message;
                }

                errored.Should().BeTrue();
                errorMessage.Should().Be(string.Format("The location for the template already exists. You can:{0} 1. Remove 'c:\\packages\\Bob'{0} 2. Use --force{0} 3. Specify a different name", Environment.NewLine));
                verifiedDirectoryPath.Should().Be("c:\\packages\\Bob");
            }
        }

        public class When_generate_is_called : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private readonly List<string> files = new List<string>();
            private readonly HashSet<string> directoryCreated = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            private readonly UTF8Encoding utf8WithoutBOM = new UTF8Encoding(false);

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(
                        (string a, string[] b) =>
                        {
                            if (a.EndsWith("templates") && b[0] == "default")
                            {
                                return "templates\\default";
                            }
                            return a + "\\" + b[0];
                        });
                fileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns<string>(dirPath => dirPath.EndsWith("templates\\default"));
                fileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8))
                    .Callback((string filePath, string fileContent, Encoding encoding) => files.Add(filePath));
                fileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), utf8WithoutBOM))
                    .Callback((string filePath, string fileContent, Encoding encoding) => files.Add(filePath));
                fileSystem.Setup(x => x.DeleteDirectoryChecked(It.IsAny<string>(), true));
                fileSystem.Setup(x => x.EnsureDirectoryExists(It.IsAny<string>())).Callback(
                    (string directory) =>
                    {
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            directoryCreated.Add(directory);
                        }
                    });
                fileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), "*.*", SearchOption.AllDirectories)).Returns(new[] { "templates\\default\\template.nuspec", "templates\\default\\random.txt" });
                fileSystem.Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns<string>(file => Path.GetDirectoryName(file));
                fileSystem.Setup(x => x.GetFileExtension(It.IsAny<string>())).Returns<string>(file => Path.GetExtension(file));
                fileSystem.Setup(x => x.ReadFile(It.IsAny<string>())).Returns(string.Empty);

                config.NewCommand.Name = "Bob";
            }

            public override void Because()
            {
                because = () => service.Generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
                files.Clear();
                directoryCreated.Clear();
            }

            [Fact]
            public void Should_generate_all_files_and_directories()
            {
                because();

                var directories = directoryCreated.ToList();
                directories.Should().HaveCount(2, "There should be 2 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\chocolatey\\Bob");
                directories.Should().HaveElementAt(1,"c:\\chocolatey\\Bob\\tools");

                files.Should().HaveCount(2, "There should be 2 files, but there was: " + string.Join(", ", files));
                files.Should().HaveElementAt(0,"c:\\chocolatey\\Bob\\__name_replace__.nuspec");
                files.Should().HaveElementAt(1,"c:\\chocolatey\\Bob\\random.txt");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\chocolatey\Bob'", Environment.NewLine));
            }

            [Fact]
            public void Should_generate_all_files_and_directories_even_with_outputdirectory()
            {
                config.OutputDirectory = "c:\\packages";

                because();

                var directories = directoryCreated.ToList();
                directories.Should().HaveCount(2, "There should be 2 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\packages\\Bob");
                directories.Should().HaveElementAt(1,"c:\\packages\\Bob\\tools");

                files.Should().HaveCount(2, "There should be 2 files, but there was: " + string.Join(", ", files));
                files.Should().HaveElementAt(0,"c:\\packages\\Bob\\__name_replace__.nuspec");
                files.Should().HaveElementAt(1,"c:\\packages\\Bob\\random.txt");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\packages\Bob'", Environment.NewLine));
            }
        }

        public class When_generate_is_called_with_nested_folders : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private readonly List<string> files = new List<string>();
            private readonly HashSet<string> directoryCreated = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            private readonly UTF8Encoding utf8WithoutBOM = new UTF8Encoding(false);

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(
                        (string a, string[] b) =>
                        {
                            if (a.EndsWith("templates") && b[0] == "test")
                            {
                                return "templates\\test";
                            }
                            return a + "\\" + b[0];
                        });
                fileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns<string>(dirPath => dirPath.EndsWith("templates\\test"));
                fileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8))
                    .Callback((string filePath, string fileContent, Encoding encoding) => files.Add(filePath));
                fileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), utf8WithoutBOM))
                    .Callback((string filePath, string fileContent, Encoding encoding) => files.Add(filePath));
                fileSystem.Setup(x => x.DeleteDirectoryChecked(It.IsAny<string>(), true));
                fileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { "templates\\test\\template.nuspec", "templates\\test\\random.txt", "templates\\test\\tools\\chocolateyInstall.ps1", "templates\\test\\tools\\lower\\another.ps1" });
                fileSystem.Setup(x => x.GetDirectories(It.IsAny<string>(), "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { "templates\\test", "templates\\test\\tools", "templates\\test\\tools\\lower" });
                fileSystem.Setup(x => x.EnsureDirectoryExists(It.IsAny<string>())).Callback(
                    (string directory) =>
                    {
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            directoryCreated.Add(directory);
                        }
                    });
                fileSystem.Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns<string>(file => Path.GetDirectoryName(file));
                fileSystem.Setup(x => x.GetFileExtension(It.IsAny<string>())).Returns<string>(file => Path.GetExtension(file));
                fileSystem.Setup(x => x.ReadFile(It.IsAny<string>())).Returns(string.Empty);

                config.NewCommand.Name = "Bob";
                config.NewCommand.TemplateName = "test";
            }

            public override void Because()
            {
                because = () => service.Generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
                files.Clear();
                directoryCreated.Clear();
            }

            [Fact]
            public void Should_generate_all_files_and_directories()
            {
                because();

                var directories = directoryCreated.ToList();
                directories.Should().HaveCount(3, "There should be 3 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\chocolatey\\Bob");
                directories.Should().HaveElementAt(1,"c:\\chocolatey\\Bob\\tools");
                directories.Should().HaveElementAt(2,"c:\\chocolatey\\Bob\\tools\\lower");

                files.Should().HaveCount(4, "There should be 4 files, but there was: " + string.Join(", ", files));
                files.Should().HaveElementAt(0,"c:\\chocolatey\\Bob\\__name_replace__.nuspec");
                files.Should().HaveElementAt(1,"c:\\chocolatey\\Bob\\random.txt");
                files.Should().HaveElementAt(2,"c:\\chocolatey\\Bob\\tools\\chocolateyInstall.ps1");
                files.Should().HaveElementAt(3,"c:\\chocolatey\\Bob\\tools\\lower\\another.ps1");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\chocolatey\Bob'", Environment.NewLine));
            }

            [Fact]
            public void Should_generate_all_files_and_directories_even_with_outputdirectory()
            {
                config.OutputDirectory = "c:\\packages";

                because();

                var directories = directoryCreated.ToList();
                directories.Should().HaveCount(3, "There should be 3 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\packages\\Bob");
                directories.Should().HaveElementAt(1,"c:\\packages\\Bob\\tools");
                directories.Should().HaveElementAt(2,"c:\\packages\\Bob\\tools\\lower");

                files.Should().HaveCount(4, "There should be 4 files, but there was: " + string.Join(", ", files));
                files.Should().HaveElementAt(0,"c:\\packages\\Bob\\__name_replace__.nuspec");
                files.Should().HaveElementAt(1,"c:\\packages\\Bob\\random.txt");
                files.Should().HaveElementAt(2,"c:\\packages\\Bob\\tools\\chocolateyInstall.ps1");
                files.Should().HaveElementAt(3,"c:\\packages\\Bob\\tools\\lower\\another.ps1");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\packages\Bob'", Environment.NewLine));
            }
        }

        public class When_generate_is_called_with_empty_nested_folders : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private readonly List<string> files = new List<string>();
            private readonly HashSet<string> directoryCreated = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            private readonly UTF8Encoding utf8WithoutBOM = new UTF8Encoding(false);

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(
                        (string a, string[] b) =>
                        {
                            if (a.EndsWith("templates") && b[0] == "test")
                            {
                                return "templates\\test";
                            }
                            return a + "\\" + b[0];
                        });
                fileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns<string>(dirPath => dirPath.EndsWith("templates\\test"));
                fileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), Encoding.UTF8))
                    .Callback((string filePath, string fileContent, Encoding encoding) => files.Add(filePath));
                fileSystem.Setup(x => x.WriteFile(It.IsAny<string>(), It.IsAny<string>(), utf8WithoutBOM))
                    .Callback((string filePath, string fileContent, Encoding encoding) => files.Add(filePath));
                fileSystem.Setup(x => x.DeleteDirectoryChecked(It.IsAny<string>(), true));
                fileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { "templates\\test\\template.nuspec", "templates\\test\\random.txt", "templates\\test\\tools\\chocolateyInstall.ps1", "templates\\test\\tools\\lower\\another.ps1" });
                fileSystem.Setup(x => x.GetDirectories(It.IsAny<string>(), "*.*", SearchOption.AllDirectories))
                    .Returns(new[] { "templates\\test", "templates\\test\\tools", "templates\\test\\tools\\lower", "templates\\test\\empty", "templates\\test\\empty\\nested" });
                fileSystem.Setup(x => x.EnsureDirectoryExists(It.IsAny<string>())).Callback(
                    (string directory) =>
                    {
                        if (!string.IsNullOrWhiteSpace(directory))
                        {
                            directoryCreated.Add(directory);
                        }
                    });
                fileSystem.Setup(x => x.GetDirectoryName(It.IsAny<string>())).Returns<string>(file => Path.GetDirectoryName(file));
                fileSystem.Setup(x => x.GetFileExtension(It.IsAny<string>())).Returns<string>(file => Path.GetExtension(file));
                fileSystem.Setup(x => x.ReadFile(It.IsAny<string>())).Returns(string.Empty);

                config.NewCommand.Name = "Bob";
                config.NewCommand.TemplateName = "test";
            }

            public override void Because()
            {
                because = () => service.Generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
                files.Clear();
                directoryCreated.Clear();
            }

            [Fact]
            public void Should_generate_all_files_and_directories()
            {
                because();

                var directories = directoryCreated.ToList();
                directories.Should().HaveCount(5, "There should be 5 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\chocolatey\\Bob");
                directories.Should().HaveElementAt(1,"c:\\chocolatey\\Bob\\tools");
                directories.Should().HaveElementAt(2,"c:\\chocolatey\\Bob\\tools\\lower");
                directories.Should().HaveElementAt(3,"c:\\chocolatey\\Bob\\empty");
                directories.Should().HaveElementAt(4,"c:\\chocolatey\\Bob\\empty\\nested");

                files.Should().HaveCount(4, "There should be 4 files, but there was: " + string.Join(", ", files));
                files.Should().HaveElementAt(0,"c:\\chocolatey\\Bob\\__name_replace__.nuspec");
                files.Should().HaveElementAt(1,"c:\\chocolatey\\Bob\\random.txt");
                files.Should().HaveElementAt(2,"c:\\chocolatey\\Bob\\tools\\chocolateyInstall.ps1");
                files.Should().HaveElementAt(3,"c:\\chocolatey\\Bob\\tools\\lower\\another.ps1");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\chocolatey\Bob'", Environment.NewLine));
            }

            [Fact]
            public void Should_generate_all_files_and_directories_even_with_outputdirectory()
            {
                config.OutputDirectory = "c:\\packages";

                because();

                var directories = directoryCreated.ToList();
                directories.Should().HaveCount(5, "There should be 5 directories, but there was: " + string.Join(", ", directories));
                directories.Should().HaveElementAt(0,"c:\\packages\\Bob");
                directories.Should().HaveElementAt(1,"c:\\packages\\Bob\\tools");
                directories.Should().HaveElementAt(2,"c:\\packages\\Bob\\tools\\lower");
                directories.Should().HaveElementAt(3,"c:\\packages\\Bob\\empty");
                directories.Should().HaveElementAt(4,"c:\\packages\\Bob\\empty\\nested");

                files.Should().HaveCount(4, "There should be 4 files, but there was: " + string.Join(", ", files));
                files.Should().HaveElementAt(0,"c:\\packages\\Bob\\__name_replace__.nuspec");
                files.Should().HaveElementAt(1,"c:\\packages\\Bob\\random.txt");
                files.Should().HaveElementAt(2,"c:\\packages\\Bob\\tools\\chocolateyInstall.ps1");
                files.Should().HaveElementAt(3,"c:\\packages\\Bob\\tools\\lower\\another.ps1");

                MockLogger.MessagesFor(LogLevel.Info).Last().Should().Be(string.Format(@"Successfully generated Bob package specification files{0} at 'c:\packages\Bob'", Environment.NewLine));
            }
        }

        public class When_generate_is_called_with_defaulttemplatename_in_configuration_but_template_folder_doesnt_exist : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });

                config.NewCommand.Name = "Bob";
                config.DefaultTemplateName = "msi";
            }

            public override void Because()
            {
                because = () => service.Generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_null_value_for_template()
            {
                because();

                config.NewCommand.TemplateName.Should().BeNull();
            }
        }

        public class When_generate_is_called_with_defaulttemplatename_in_configuration_and_template_folder_exists : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private string verifiedDirectoryPath;

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                fileSystem.Setup(x => x.DirectoryExists(Path.Combine(ApplicationParameters.TemplatesLocation, "msi"))).Returns<string>(
                    x =>
                    {
                        verifiedDirectoryPath = x;
                        return true;
                    });

                config.NewCommand.Name = "Bob";
                config.DefaultTemplateName = "msi";
            }

            public override void Because()
            {
                because = () => service.Generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_template_name_from_configuration()
            {
                because();

                config.NewCommand.TemplateName.Should().Be("msi");
            }
        }

        public class When_generate_is_called_with_defaulttemplatename_in_configuration_and_template_name_option_set : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private string verifiedDirectoryPath;

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                fileSystem.Setup(x => x.DirectoryExists(Path.Combine(ApplicationParameters.TemplatesLocation, "zip"))).Returns<string>(
                    x =>
                    {
                        verifiedDirectoryPath = x;
                        return true;
                    });

                config.NewCommand.Name = "Bob";
                config.NewCommand.TemplateName = "zip";
                config.DefaultTemplateName = "msi";
            }

            public override void Because()
            {
                because = () => service.Generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_template_name_from_command_line_option()
            {
                because();

                config.NewCommand.TemplateName.Should().Be("zip");
            }
        }

        public class When_generate_is_called_with_built_in_option_set : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();

                config.NewCommand.Name = "Bob";
                config.NewCommand.UseOriginalTemplate = true;
            }

            public override void Because()
            {
                because = () => service.Generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_null_value_for_template()
            {
                because();

                config.NewCommand.TemplateName.Should().BeNull();
            }
        }

        public class When_generate_is_called_with_built_in_option_set_and_defaulttemplate_in_configuration : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();

                config.NewCommand.Name = "Bob";
                config.NewCommand.UseOriginalTemplate = true;
                config.DefaultTemplateName = "msi";
            }

            public override void Because()
            {
                because = () => service.Generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_null_value_for_template()
            {
                because();

                config.NewCommand.TemplateName.Should().BeNull();
            }
        }

        public class When_generate_is_called_with_built_in_option_set_and_template_name_option_set_and_template_folder_exists : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private string verifiedDirectoryPath;

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                fileSystem.Setup(x => x.DirectoryExists(Path.Combine(ApplicationParameters.TemplatesLocation, "zip"))).Returns<string>(
                    x =>
                    {
                        verifiedDirectoryPath = x;
                        return true;
                    });

                config.NewCommand.Name = "Bob";
                config.NewCommand.TemplateName = "zip";
                config.NewCommand.UseOriginalTemplate = true;
            }

            public override void Because()
            {
                because = () => service.Generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_template_name_from_command_line_option()
            {
                because();

                config.NewCommand.TemplateName.Should().Be("zip");
            }
        }

        public class When_generate_is_called_with_built_in_option_set_and_template_name_option_set_and_defaulttemplatename_set_and_template_folder_exists : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private string verifiedDirectoryPath;

            public override void Context()
            {
                base.Context();

                fileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\chocolatey");
                fileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns((string a, string[] b) => { return a + "\\" + b[0]; });
                fileSystem.Setup(x => x.DirectoryExists(Path.Combine(ApplicationParameters.TemplatesLocation, "zip"))).Returns<string>(
                    x =>
                    {
                        verifiedDirectoryPath = x;
                        return true;
                    });

                config.NewCommand.Name = "Bob";
                config.NewCommand.TemplateName = "zip";
                config.DefaultTemplateName = "msi";
                config.NewCommand.UseOriginalTemplate = true;
            }

            public override void Because()
            {
                because = () => service.Generate(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_use_template_name_from_command_line_option()
            {
                because();

                config.NewCommand.TemplateName.Should().Be("zip");
            }
        }

        public class When_list_noop_is_called : TemplateServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();

            public override void Because()
            {
                because = () => service.ListDryRun(config);
            }

            public override void BeforeEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            public void Should_log_template_location_if_no_template_name()
            {
                because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0,"Would have listed templates in {0}".FormatWith(ApplicationParameters.TemplatesLocation));
            }

            [Fact]
            public void Should_log_template_name_if_template_name()
            {
                config.TemplateCommand.Name = "msi";
                because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0,"Would have listed information about {0}".FormatWith(config.TemplateCommand.Name));
            }
        }
    }
}
