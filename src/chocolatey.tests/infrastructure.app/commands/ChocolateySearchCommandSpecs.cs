// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.tests.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using FluentAssertions;

    public class ChocolateySearchCommandSpecs
    {
        [ConcernFor("search")]
        public abstract class ChocolateySearchCommandSpecsBase : TinySpec
        {
            protected ChocolateySearchCommand command;
            protected Mock<IChocolateyPackageService> packageService = new Mock<IChocolateyPackageService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                configuration.Sources = "bob";
                command = new ChocolateySearchCommand(packageService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateySearchCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_not_implement_list()
            {
                results.Should().NotContain("list");
            }

            [Fact]
            public void Should_implement_search()
            {
                results.Should().Contain("search");
            }

            [Fact]
            public void Should_implement_find()
            {
                results.Should().Contain("find");
            }
        }

        public class When_configurating_the_argument_parser_for_search_command : ChocolateySearchCommandSpecsBase
        {
            private OptionSet optionSet;

            public override void Context()
            {
                base.Context();
                optionSet = new OptionSet();
                configuration.CommandName = "search";
            }

            public override void Because()
            {
                command.ConfigureArgumentParser(optionSet, configuration);
            }

            [Fact]
            public void Should_add_source_to_the_option_set()
            {
                optionSet.Contains("source").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_source_to_the_option_set()
            {
                optionSet.Contains("s").Should().BeTrue();
            }

            [Fact]
            public void Should_add_prerelease_to_the_option_set()
            {
                optionSet.Contains("prerelease").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_prerelease_to_the_option_set()
            {
                optionSet.Contains("pre").Should().BeTrue();
            }

            [Fact]
            public void Should_add_includeprograms_to_the_option_set()
            {
                optionSet.Contains("includeprograms").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_includeprograms_to_the_option_set()
            {
                optionSet.Contains("i").Should().BeTrue();
            }

            [Fact]
            public void Should_add_allversions_to_the_option_set()
            {
                optionSet.Contains("allversions").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_allversions_to_the_option_set()
            {
                optionSet.Contains("a").Should().BeTrue();
            }

            [Fact]
            public void Should_add_user_to_the_option_set()
            {
                optionSet.Contains("user").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_user_to_the_option_set()
            {
                optionSet.Contains("u").Should().BeTrue();
            }

            [Fact]
            public void Should_add_password_to_the_option_set()
            {
                optionSet.Contains("password").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_password_to_the_option_set()
            {
                optionSet.Contains("p").Should().BeTrue();
            }
        }

        [NUnit.Framework.TestFixture("search")]
        [NUnit.Framework.TestFixture("find")]
        public class When_configurating_the_argument_parser : ChocolateySearchCommandSpecsBase
        {
            private OptionSet optionSet;

            public When_configurating_the_argument_parser(string commandName)
            {
                configuration.CommandName = commandName;
            }

            public override void Context()
            {
                base.Context();
                optionSet = new OptionSet();
            }

            public override void Because()
            {
                command.ConfigureArgumentParser(optionSet, configuration);
            }

            [Fact]
            public void Should_add_source_to_the_option_set()
            {
                optionSet.Contains("source").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_source_to_the_option_set()
            {
                optionSet.Contains("s").Should().BeTrue();
            }

            [Fact]
            public void Should_add_prerelease_to_the_option_set()
            {
                optionSet.Contains("prerelease").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_prerelease_to_the_option_set()
            {
                optionSet.Contains("pre").Should().BeTrue();
            }

            [Fact]
            public void Should_add_includeprograms_to_the_option_set()
            {
                optionSet.Contains("includeprograms").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_includeprograms_to_the_option_set()
            {
                optionSet.Contains("i").Should().BeTrue();
            }

            [Fact]
            public void Should_add_allversions_to_the_option_set()
            {
                optionSet.Contains("allversions").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_allversions_to_the_option_set()
            {
                optionSet.Contains("a").Should().BeTrue();
            }

            [Fact]
            public void Should_add_user_to_the_option_set()
            {
                optionSet.Contains("user").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_user_to_the_option_set()
            {
                optionSet.Contains("u").Should().BeTrue();
            }

            [Fact]
            public void Should_add_password_to_the_option_set()
            {
                optionSet.Contains("password").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_password_to_the_option_set()
            {
                optionSet.Contains("p").Should().BeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateySearchCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private readonly string source = "https://somewhereoutthere";
            private Action because;

            public override void Context()
            {
                base.Context();
                unparsedArgs.Add("pkg1");
                unparsedArgs.Add("pkg2");
                configuration.Sources = source;
            }

            public override void Because()
            {
                because = () => command.ParseAdditionalArguments(unparsedArgs, configuration);
            }

            [Fact]
            public void Should_set_unparsed_arguments_to_configuration_input()
            {
                because();
                configuration.Input.Should().Be("pkg1 pkg2");
            }

            [Fact]
            public void Should_leave_source_as_set()
            {
                configuration.ListCommand.LocalOnly = false;
                because();
                configuration.Sources.Should().Be(source);
            }
        }

        public class When_noop_is_called_with_search_command : ChocolateySearchCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                configuration.CommandName = "search";
            }

            public override void Because()
            {
                command.DryRun(configuration);
            }

            [Fact]
            public void Should_call_service_list_noop()
            {
                packageService.Verify(c => c.ListDryRun(configuration), Times.Once);
            }
        }

        public class When_noop_is_called : ChocolateySearchCommandSpecsBase
        {
            public override void Because()
            {
                command.DryRun(configuration);
            }

            [Fact]
            public void Should_call_service_list_noop()
            {
                packageService.Verify(c => c.ListDryRun(configuration), Times.Once);
            }

            [Fact]
            public void Should_not_report_any_warning_messages()
            {
                MockLogger.Messages.Keys.Should().NotContain("Warn");
            }
        }

        public class When_run_is_called_with_search_command : ChocolateySearchCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                configuration.CommandName = "search";
            }

            public override void Because()
            {
                command.Run(configuration);
            }

            [Fact]
            public void Should_call_service_list_run()
            {
                packageService.Verify(c => c.List(configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateySearchCommandSpecsBase
        {
            public override void Because()
            {
                command.Run(configuration);
            }

            [Fact]
            public void Should_call_service_list_run()
            {
                packageService.Verify(c => c.List(configuration), Times.Once);
            }

            [Fact]
            public void Should_not_report_any_warning_messages()
            {
                MockLogger.Messages.Keys.Should().NotContain("Warn");
            }
        }

        [NUnit.Framework.TestFixture("search")]
        [NUnit.Framework.TestFixture("find")]
        public class When_outputting_help_message : ChocolateySearchCommandSpecsBase
        {
            public When_outputting_help_message(string commandName)
            {
                configuration.CommandName = commandName;
            }

            public override void Because()
            {
                command.HelpMessage(configuration);
            }
        }
    }
}
