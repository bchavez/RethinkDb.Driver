using System;
using FluentAssertions;
using NUnit.Framework;

namespace RethinkDb.Driver.ReGrid.Tests
{
    [TestFixture]
    public class PathTests
    {
        private string filename = "foobar.mp3";

        [Test]
        public void non_rooted_file_gets_rooted()
        {
            filename.SafePath().Should().Be("/foobar.mp3");
        }

        [Test]
        public void a_safe_path_is_not_a_directory()
        {
            Action act = () => "/foobar/".SafePath();

            act.ShouldThrow<InvalidPathException>();
        }
    }

}