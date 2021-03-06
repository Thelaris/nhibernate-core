﻿using System.Linq;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.GH1879
{
	[TestFixture]
	public class NestedConditionalThenMethodCall : GH1879BaseFixture<Employee>
	{
		/// <inheritdoc />
		protected override void OnSetUp()
		{
			using (var session = OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var clientA = new Client { Name = "Alpha" };
				var clientB = new Client { Name = "Beta" };
				session.Save(clientA);
				session.Save(clientB);

				var projectA = new Project { Name = "Apple" };
				var projectB = new Project { Name = "Banana" };
				var projectC = new Project { Name = "Cherry" };
				session.Save(projectA);
				session.Save(projectB);
				session.Save(projectC);

				var issue1 = new Issue { Name = "1", Client = null, Project = null };
				var issue2 = new Issue { Name = "2", Client = clientA, Project = projectA };
				var issue3 = new Issue { Name = "3", Client = clientA, Project = projectA };
				var issue4 = new Issue { Name = "4", Client = clientA, Project = projectB };
				var issue5 = new Issue { Name = "5", Client = clientB, Project = projectC };
				session.Save(issue1);
				session.Save(issue2);
				session.Save(issue3);
				session.Save(issue4);
				session.Save(issue5);

				session.Save(new Employee { Name = "Andy", ReviewAsPrimary = true, ReviewIssues = { issue1, issue2, issue5 }, WorkIssues = { issue3 }, Projects = { projectA, projectB } });
				session.Save(new Employee { Name = "Bart", ReviewAsPrimary = false, ReviewIssues = { issue3 }, WorkIssues = { issue4, issue5 }, Projects = { projectB, projectC } });
				session.Save(new Employee { Name = "Carl", ReviewAsPrimary = true, ReviewIssues = { issue3 }, WorkIssues = { issue1, issue4, issue5 }, Projects = { projectC } });
				session.Save(new Employee { Name = "Dorn", ReviewAsPrimary = false, ReviewIssues = { issue3 }, WorkIssues = { issue1, issue4 }, Projects = { } });

				session.Flush();
				transaction.Commit();
			}
		}

		[Test]
		public void WhereClause()
		{
			AreEqual(
				// Conditional style
				q => q.Where(e => (e.ReviewAsPrimary ? e.ReviewIssues : e.Projects.Any() ? e.Projects.SelectMany(x => x.Issues) : e.WorkIssues).Any(i => i.Client.Name == "Beta")),
			    // Expected
			    q => q.Where(e => e.ReviewAsPrimary ? e.ReviewIssues.Any(i => i.Client.Name == "Beta") : e.Projects.Any() ? e.Projects.SelectMany(x => x.Issues).Any(i => i.Client.Name == "Beta") : e.WorkIssues.Any(i => i.Client.Name == "Beta"))
			);
		}

		[Test]
		public void SelectClause()
		{
			AreEqual(
				// Conditional style
				q => q.OrderBy(e => e.Name)
				      .Select(e => (e.ReviewAsPrimary ? e.ReviewIssues : e.Projects.Any() ? e.Projects.SelectMany(x => x.Issues) : e.WorkIssues).Any(i => i.Client.Name == "Beta")),
				// Expected
				q => q.OrderBy(e => e.Name)
				      .Select(e => e.ReviewAsPrimary ? e.ReviewIssues.Any(i => i.Client.Name == "Beta") : e.Projects.Any() ? e.Projects.SelectMany(x => x.Issues).Any(i => i.Client.Name == "Beta") : e.WorkIssues.Any(i => i.Client.Name == "Beta"))
			);
		}

		[Test]
		public void SelectClauseToAnon()
		{
			AreEqual(
				// Conditional style
				q => q.OrderBy(e => e.Name)
				      .Select(e => new { e.Name, Beta = (e.ReviewAsPrimary ? e.ReviewIssues : e.Projects.Any() ? e.Projects.SelectMany(x => x.Issues) : e.WorkIssues).Any(i => i.Client.Name == "Beta") }),
				// Expected
				q => q.OrderBy(e => e.Name)
				      .Select(e => new { e.Name, Beta = e.ReviewAsPrimary ? e.ReviewIssues.Any(i => i.Client.Name == "Beta") : e.Projects.Any() ? e.Projects.SelectMany(x => x.Issues).Any(i => i.Client.Name == "Beta") : e.WorkIssues.Any(i => i.Client.Name == "Beta") })
			);
		}

		[Test]
		public void OrderByClause()
		{
			AreEqual(
				// Conditional style
				q => q.OrderBy(e => (e.ReviewAsPrimary ? e.ReviewIssues : e.Projects.Any() ? e.Projects.SelectMany(x => x.Issues) : e.WorkIssues).Count())
				      .ThenBy(p => p.Name)
				      .Select(p => p.Name),
				// Expected
				q => q.OrderBy(e => e.ReviewAsPrimary ? e.ReviewIssues.Count() : e.Projects.Any() ? e.Projects.SelectMany(x => x.Issues).Count() : e.WorkIssues.Count())
				      .ThenBy(p => p.Name)
				      .Select(p => p.Name)
			);
		}

		[Test]
		public void GroupByClause()
		{
			AreEqual(
				// Conditional style
				q => q.GroupBy(e => (e.ReviewAsPrimary ? e.ReviewIssues : e.Projects.Any() ? e.Projects.SelectMany(x => x.Issues) : e.WorkIssues).Count())
				      .OrderBy(x => x.Key)
				      .Select(grp => grp.Count()),
				// Expected
				q => q.GroupBy(e => e.ReviewAsPrimary ? e.ReviewIssues.Count() : e.Projects.Any() ? e.Projects.SelectMany(x => x.Issues).Count() : e.WorkIssues.Count())
				      .OrderBy(x => x.Key)
				      .Select(grp => grp.Count())
			);
		}
	}
}
