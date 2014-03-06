/**
 * Couchbase Lite for .NET
 *
 * Original iOS version by Jens Alfke
 * Android Port by Marty Schoch, Traun Leyden
 * C# Port by Zack Gramana
 *
 * Copyright (c) 2012, 2013, 2014 Couchbase, Inc. All rights reserved.
 * Portions (c) 2013, 2014 Xamarin, Inc. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file
 * except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software distributed under the
 * License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
 * either express or implied. See the License for the specific language governing permissions
 * and limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Apache.Http;
using Apache.Http.Client;
using Apache.Http.Impl.Client;
using Couchbase.Lite;
using Couchbase.Lite.Support;
using Couchbase.Lite.Util;
using Sharpen;

namespace Couchbase.Lite.Support
{
	public class RemoteMultipartDownloaderRequest : RemoteRequest
	{
		private Database db;

		public RemoteMultipartDownloaderRequest(ScheduledExecutorService workExecutor, HttpClientFactory
			 clientFactory, string method, Uri url, object body, Database db, IDictionary<string
			, object> requestHeaders, RemoteRequestCompletionBlock onCompletion) : base(workExecutor
			, clientFactory, method, url, body, requestHeaders, onCompletion)
		{
			this.db = db;
		}

		public override void Run()
		{
			HttpClient httpClient = clientFactory.GetHttpClient();
			PreemptivelySetAuthCredentials(httpClient);
			HttpRequestMessage request = CreateConcreteRequest();
			request.AddHeader("Accept", "*/*");
			AddRequestHeaders(request);
			ExecuteRequest(httpClient, request);
		}

		protected internal override void ExecuteRequest(HttpClient httpClient, HttpRequestMessage
			 request)
		{
			object fullBody = null;
			Exception error = null;
			try
			{
				HttpResponse response = httpClient.Execute(request);
				try
				{
					// add in cookies to global store
					if (httpClient is DefaultHttpClient)
					{
						DefaultHttpClient defaultHttpClient = (DefaultHttpClient)httpClient;
						CouchbaseLiteHttpClientFactory.Instance.AddCookies(defaultHttpClient.GetCookieStore
							().GetCookies());
					}
				}
				catch (Exception e)
				{
					Log.E(Database.Tag, "Unable to add in cookies to global store", e);
				}
				StatusLine status = response.GetStatusLine();
				if (status.GetStatusCode() >= 300)
				{
					Log.E(Database.Tag, "Got error " + Sharpen.Extensions.ToString(status.GetStatusCode
						()));
					Log.E(Database.Tag, "Request was for: " + request.ToString());
					Log.E(Database.Tag, "Status reason: " + status.GetReasonPhrase());
					error = new HttpResponseException(status.GetStatusCode(), status.GetReasonPhrase(
						));
				}
				else
				{
					HttpEntity entity = response.GetEntity();
					Header contentTypeHeader = entity.GetContentType();
					InputStream inputStream = null;
					if (contentTypeHeader != null && contentTypeHeader.GetValue().Contains("multipart/related"
						))
					{
						try
						{
							MultipartDocumentReader reader = new MultipartDocumentReader(response, db);
							reader.SetContentType(contentTypeHeader.GetValue());
							inputStream = entity.GetContent();
							int bufLen = 1024;
							byte[] buffer = new byte[bufLen];
							int numBytesRead = 0;
							while ((numBytesRead = inputStream.Read(buffer)) != -1)
							{
								if (numBytesRead != bufLen)
								{
									byte[] bufferToAppend = Arrays.CopyOfRange(buffer, 0, numBytesRead);
									reader.AppendData(bufferToAppend);
								}
								else
								{
									reader.AppendData(buffer);
								}
							}
							reader.Finish();
							fullBody = reader.GetDocumentProperties();
							RespondWithResult(fullBody, error);
						}
						finally
						{
							try
							{
								inputStream.Close();
							}
							catch (IOException)
							{
							}
						}
					}
					else
					{
						if (entity != null)
						{
							try
							{
								inputStream = entity.GetContent();
								fullBody = Manager.GetObjectMapper().ReadValue<object>(inputStream);
								RespondWithResult(fullBody, error);
							}
							finally
							{
								try
								{
									inputStream.Close();
								}
								catch (IOException)
								{
								}
							}
						}
					}
				}
			}
			catch (ClientProtocolException e)
			{
				Log.E(Database.Tag, "client protocol exception", e);
				error = e;
			}
			catch (IOException e)
			{
				Log.E(Database.Tag, "io exception", e);
				error = e;
			}
		}
	}
}
