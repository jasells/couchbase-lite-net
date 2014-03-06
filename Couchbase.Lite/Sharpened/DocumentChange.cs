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
using Couchbase.Lite;
using Couchbase.Lite.Internal;
using Couchbase.Lite.Util;
using Sharpen;

namespace Couchbase.Lite
{
	/// <summary>Provides details about a Document change.</summary>
	/// <remarks>Provides details about a Document change.</remarks>
	public class DocumentChange
	{
		/// <exclude></exclude>
		[InterfaceAudience.Private]
		internal DocumentChange(RevisionInternal addedRevision, RevisionInternal winningRevision
			, bool isConflict, Uri sourceUrl)
		{
			this.addedRevision = addedRevision;
			this.winningRevision = winningRevision;
			this.isConflict = isConflict;
			this.sourceUrl = sourceUrl;
		}

		private RevisionInternal addedRevision;

		private RevisionInternal winningRevision;

		private bool isConflict;

		private Uri sourceUrl;

		[InterfaceAudience.Public]
		public virtual string GetDocumentId()
		{
			return addedRevision.GetDocId();
		}

		[InterfaceAudience.Public]
		public virtual string GetRevisionId()
		{
			return addedRevision.GetRevId();
		}

		[InterfaceAudience.Public]
		public virtual bool IsCurrentRevision()
		{
			return winningRevision != null && addedRevision.GetRevId().Equals(winningRevision
				.GetRevId());
		}

		[InterfaceAudience.Public]
		public virtual bool IsConflict()
		{
			return isConflict;
		}

		[InterfaceAudience.Public]
		public virtual Uri GetSourceUrl()
		{
			return sourceUrl;
		}

		/// <exclude></exclude>
		[InterfaceAudience.Private]
		public virtual RevisionInternal GetAddedRevision()
		{
			return addedRevision;
		}

		/// <exclude></exclude>
		[InterfaceAudience.Private]
		internal virtual RevisionInternal GetWinningRevision()
		{
			return winningRevision;
		}

		[InterfaceAudience.Public]
		public override string ToString()
		{
			try
			{
				return string.Format("docId: %s rev: %s isConflict: %s sourceUrl: %s", GetDocumentId
					(), GetRevisionId(), IsConflict(), GetSourceUrl());
			}
			catch (Exception e)
			{
				Log.E(Database.Tag, "Error in DocumentChange.toString()", e);
				return base.ToString();
			}
		}
	}
}
