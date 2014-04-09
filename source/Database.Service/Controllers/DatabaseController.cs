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
		private const string DynamicAssemblyName = "Database.DynamicMappings";

		private static readonly MethodInfo OpenGenericCreateQueryableMethod = ReflectionUtility.GetMethodInfo((DatabaseController x) => x.CreateQueryable<string>(null, null));

		private readonly IQueryExecutor _queryExecutor;
		private readonly ITableReflector _tableReflector;
		private readonly IDynamicAssemblyManager _dynamicAssemblyManager;

		public DatabaseController(IQueryExecutor queryExecutor, ITableReflector tableReflector, IDynamicAssemblyManagerFactory dynamicAssemblyManagerFactory)
		{
			_tableReflector = tableReflector;
			_dynamicAssemblyManager = dynamicAssemblyManagerFactory.Create(DynamicAssemblyName);
			_queryExecutor = queryExecutor;
		}

		[HttpGet]
		[Queryable]
		[Route("api/{serverType}/{serverName}/{instanceName}/{databaseName}/{tableName}", Name = "ServerInstanceDatabaseTable")]
		public HttpResponseMessage Get(string serverType, string serverName, string instanceName, string databaseName, string tableName)
		{
			var serverNameWithInstance = String.Format(@"{0}\{1}", serverName, instanceName);

			return Get(serverType, serverNameWithInstance, databaseName, tableName);
		}

		[HttpGet]
		[Queryable]
		[Route("api/{serverType}/{serverName}/{databaseName}/{tableName}", Name = "ServerDatabaseTable")]
		public HttpResponseMessage Get(string serverType, string serverName, string databaseName, string tableName)
		{
			DatabaseType result;
			if (Enums.TryParse(serverType, out result) == false)
			{
				throw new ArgumentException(String.Format("'{0}' is an unrecognized database type.", serverType), "serverType");
			}

			var database = new Core.Database
			{
				ServerName = serverName,
				DatabaseName = databaseName,
				DatabaseType = result
			};

			var tableDefinition = _tableReflector.GetTableDefinition(database, tableName);

			var types = _dynamicAssemblyManager.BuildTypesFor(tableDefinition);

			// doing this some other way (e.g. putting the reflection inside a class and using the non-generic System.Collections.IQueryable)
			// causes an error at some point in the ASP.NET pipeline so that the server returns a 500; i believe it is a serialization error
			// possibly having to do with some kind of XML linking (i've seen errors similar to this about unexpected types and whatnot)
			var genericMethod = OpenGenericCreateQueryableMethod.MakeGenericMethod(types.EntityType);

			var response = (HttpResponseMessage) genericMethod.Invoke(this, new object[] { database, types });

			return response;
		}

		[HttpGet]
		[Queryable]
		[Route("api/{serverType}/{databaseName}/{tableName}", Name = "DatabaseTable")]
		public HttpResponseMessage Get(string serverType, string databaseName, string tableName)
		{
			return Get(serverType, Environment.MachineName, databaseName, tableName);
		}

		private HttpResponseMessage CreateQueryable<T>(Core.Database database, EntityTypes types)
		{
			var queryable = _queryExecutor.Execute<T>(database, types);

			var response = Request.CreateResponse(HttpStatusCode.OK, queryable);

			return response;
		}
	}
}
