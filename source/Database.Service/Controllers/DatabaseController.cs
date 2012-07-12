using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

using Database.Core;
using Database.Core.Querying;
using Database.Core.TableReflection;
using Database.Core.TypeBuilding;

namespace Database.Service.Controllers
{
	public class DatabaseController : ApiController
	{
		private static readonly MethodInfo OpenGenericCreateQueryableMethod = typeof (DatabaseController).GetMethod("CreateQueryable", BindingFlags.Instance | BindingFlags.NonPublic);

		private readonly IQueryExecutor _queryExecutor;
		private readonly ITableReflector _tableReflector;
		private readonly IDynamicAssemblyManager _dynamicAssemblyManager;

		public DatabaseController(IQueryExecutor queryExecutor, ITableReflector tableReflector, IDynamicAssemblyManager dynamicAssemblyManager)
		{
			_tableReflector = tableReflector;
			_dynamicAssemblyManager = dynamicAssemblyManager;
			_queryExecutor = queryExecutor;
		}

		[Queryable]
		public HttpResponseMessage Get(string serverName, string instanceName, string databaseName, string tableName)
		{
			var serverNameWithInstance = String.Format(@"{0}\{1}", serverName, instanceName);

			return Get(serverNameWithInstance, databaseName, tableName);
		}

		[Queryable]
		public HttpResponseMessage Get(string serverName, string databaseName, string tableName)
		{
			var database = new Core.Database
			{
				ServerName = serverName,
				DatabaseName = databaseName,
				DatabaseType = DatabaseType.SqlServer
			};

			var tableDefinition = _tableReflector.GetTableDefinition(database, tableName);

			var types = _dynamicAssemblyManager.BuildTypesFor(tableDefinition);

			// doing this some other way (e.g. putting the reflection inside a class and using IQueryable) causes an error
			// at some point in the pipeline so that the server returns a 500; i believe it is a serialization error
			// possibly having to do with some kind of XML linking (i've seen errors similar to this about unexpected types
			// and whatnot)
			var genericMethod = OpenGenericCreateQueryableMethod.MakeGenericMethod(types.EntityType);

			var response = (HttpResponseMessage) genericMethod.Invoke(this, new object[] { database, types });

			return response;
		}

		[Queryable]
		public HttpResponseMessage Get(string databaseName, string tableName)
		{
			return Get(Environment.MachineName, databaseName, tableName);
		}

		private HttpResponseMessage CreateQueryable<T>(Core.Database database, EntityTypes types)
		{
			var queryable = _queryExecutor.Execute<T>(database, types);

			var response = Request.CreateResponse(HttpStatusCode.OK, queryable);

			return response;
		}
	}
}
