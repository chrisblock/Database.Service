using System;
using System.Collections;
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

			var entityType = query.GetEntityType();
			var mappingType = query.GetMappingType();

			var configuration = new Configuration()
				.DataBaseIntegration(x =>
				{
					x.Dialect<MsSql2008Dialect>();
					x.Driver<SqlClientDriver>();
					x.ConnectionString = connect.GetConnectionString();
				});

			IList result;

			var hmm = Type.GetType(mappingType.AssemblyQualifiedName);

			// TODO: once Assembly.Load works on the dynamic assembly, then NHibernate will be able to find the entity type
			//var assemblyString = mappingType.Assembly.FullName;
			//var codeBase = mappingType.Assembly.CodeBase;
			//var wat = Assembly.Load(assemblyString);

			var desktop = Path.Combine(System.Environment.GetEnvironmentVariable("USERPROFILE"), "Desktop");

			using (var sessionFactory = Fluently.Configure(configuration)
				.Diagnostics(diagnostics => diagnostics.Enable(true).OutputToFile(Path.Combine(desktop, "Fluent.log")))
				.Mappings(mappings => mappings.FluentMappings.Add(mappingType).ExportTo(desktop))
				.BuildSessionFactory())
			{
				using (var session = sessionFactory.OpenStatelessSession())
				{
					result = session.CreateCriteria(entityType).List();
				}
			}

			var response = new HttpResponseMessage<IList>(result, HttpStatusCode.OK);

			return response;
		}

		public HttpResponseMessage Get(string databaseName, string tableName)
		{
			return Get(System.Environment.MachineName, databaseName, tableName);
		}
	}
}
