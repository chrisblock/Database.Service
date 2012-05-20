using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

using Database.Core;

using FluentNHibernate.Cfg;

using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;

using Configuration = NHibernate.Cfg.Configuration;

namespace Database.Service.Controllers
{
	public class DatabaseController : ApiController
	{
		public HttpResponseMessage Get(string serverName, string instanceName, string databaseName, string tableName)
		{
			var serverNameWithInstance = String.Format("{0}\\{1}", serverName, instanceName);

			return Get(serverNameWithInstance, databaseName, tableName);
		}

		public HttpResponseMessage Get(string serverName, string databaseName, string tableName)
		{
			var connect = Connect.To(serverName, databaseName);

			var query = connect.CreateQuery(tableName);

			var type = GetType();

			var genericGetMethod = type.GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic);

			var getMethod = genericGetMethod.MakeGenericMethod(query.GetEntityType());

			var result = (HttpResponseMessage) getMethod.Invoke(this, new object[] { query });

			return result;
		}

		public HttpResponseMessage Get(string databaseName, string tableName)
		{
			return Get(System.Environment.MachineName, databaseName, tableName);
		}

		private HttpResponseMessage<IEnumerable<T>> Get<T>(Query query)
			where T : class
		{
			var mappingType = query.GetMappingType();

			var configuration = new Configuration()
				.DataBaseIntegration(x =>
				{
					x.Dialect<MsSql2008Dialect>();
					x.Driver<SqlClientDriver>();
					x.ConnectionString = query.GetConnectionString();
				});

			IEnumerable<T> result;

			var desktop = Path.Combine(System.Environment.GetEnvironmentVariable("USERPROFILE"), "Desktop");

			using (var sessionFactory = Fluently.Configure(configuration)
				.Diagnostics(diagnostics => diagnostics.Enable(true).OutputToFile(Path.Combine(desktop, "Fluent.log")))
				.Mappings(mappings => mappings.FluentMappings.Add(mappingType).ExportTo(desktop))
				.BuildSessionFactory())
			{
				using (var session = sessionFactory.OpenStatelessSession())
				{
					result = session.QueryOver<T>()
						.List();
				}
			}

			var response = new HttpResponseMessage<IEnumerable<T>>(result, HttpStatusCode.OK);

			return response;
		}
	}
}
