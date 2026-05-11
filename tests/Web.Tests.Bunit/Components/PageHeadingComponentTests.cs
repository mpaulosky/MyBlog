//=======================================================
//Copyright (c) 2026. All rights reserved.
//File Name :     PageHeadingComponentTests.cs
//Company :       mpaulosky
//Author :        Matthew Paulosky
//Solution Name : MyBlog
//Project Name :  Web.Tests.Bunit
//=======================================================

using MyBlog.Web.Components.Shared;

namespace Web.Components;

public class PageHeadingComponentTests : BunitContext
{
	[Theory]
	[InlineData("1", "h1")]
	[InlineData("2", "h2")]
	[InlineData("3", "h3")]
	[InlineData("4", "h4")]
	public void PageHeadingRendersRequestedHeadingLevel(string level, string selector)
	{
		// Arrange (none)
		// Act
		var cut = Render<PageHeadingComponent>(parameters => parameters
				.Add(component => component.HeaderText, "Section heading")
				.Add(component => component.Level, level)
				.Add(component => component.TextColorClass, "text-primary-900 dark:text-primary-50"));

		// Assert
		var heading = cut.Find($"header {selector}");

		heading.TextContent.Trim().Should().Be("Section heading");
		heading.GetAttribute("class").Should().Contain("text-primary-900").And.Contain("dark:text-primary-50");
		cut.FindAll("header h1, header h2, header h3, header h4").Should().ContainSingle();
	}

	[Fact]
	public void PageHeadingDefaultsToLevelOneWhenLevelIsOmitted()
	{
		// Arrange (none)
		// Act
		var cut = Render<PageHeadingComponent>(parameters => parameters
				.Add(component => component.HeaderText, "Default heading"));

		// Assert
		cut.Find("header h1").TextContent.Trim().Should().Be("Default heading");
		cut.FindAll("header h2, header h3, header h4").Should().BeEmpty();
	}

	[Fact]
	public void PageHeadingFallsBackToLevelOneWhenLevelIsUnsupported()
	{
		// Arrange (none)
		// Act
		var cut = Render<PageHeadingComponent>(parameters => parameters
				.Add(component => component.HeaderText, "Fallback heading")
				.Add(component => component.Level, "99")
				.Add(component => component.TextColorClass, "text-primary-900 dark:text-primary-50"));

		// Assert
		var heading = cut.Find("header h1");

		heading.TextContent.Trim().Should().Be("Fallback heading");
		heading.GetAttribute("class").Should().Contain("text-primary-900").And.Contain("dark:text-primary-50");
		cut.FindAll("header h2, header h3, header h4").Should().BeEmpty();
	}
}
