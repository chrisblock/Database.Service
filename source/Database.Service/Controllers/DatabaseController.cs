using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;

using Database.Core;

namespace Database.Service.Controllers
{
	public class DatabaseController : ApiController
	{
		private static readonly MethodInfo GenericGetMethod = typeof (DatabaseController).GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic);

		public HttpResponseMessage Get(string serverName, string instanceName, string databaseName, string tableName)
		{
			var serverNameWithInstance = String.Format(@"{0}\{1}", serverName, instanceName);

			return Get(serverNameWithInstance, databaseName, tableName);
		}

		public HttpResponseMessage Get(string serverName, string databaseName, string tableName)
		{
			var connect = Connect.To(serverName, databaseName);

			var query = connect.CreateQuery(tableName);

			var getMethod = GenericGetMethod.MakeGenericMethod(query.GetEntityType());

			var result = (HttpResponseMessage) getMethod.Invoke(this, new object[] { query });

			return result;
		}

		public HttpResponseMessage Get(string databaseName, string tableName)
		{
			return Get(Environment.MachineName, databaseName, tableName);
		}

		private HttpResponseMessage<IEnumerable<T>> Get<T>(Query query) where T : class
		{
			IEnumerable<T> result = query.Execute<T>();

			var response = new HttpResponseMessage<IEnumerable<T>>(result, HttpStatusCode.OK);

			return response;
		}
	}
}
