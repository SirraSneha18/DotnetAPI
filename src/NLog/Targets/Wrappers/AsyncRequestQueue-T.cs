// 
// Copyright (c) 2004-2016 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions 
// are met:
// 
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer. 
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution. 
// 
// * Neither the name of Jaroslaw Kowalski nor the names of its 
//   contributors may be used to endorse or promote products derived from this
//   software without specific prior written permission. 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
// THE POSSIBILITY OF SUCH DAMAGE.
// 

namespace NLog.Targets.Wrappers
{
    using System;
    using System.Collections.Generic;
    using NLog.Common;

    /// <summary>
    /// Asynchronous request queue.
    /// </summary>
	internal class AsyncRequestQueue
    {
        private readonly Queue<AsyncLogEventInfo> logEventInfoQueue = new Queue<AsyncLogEventInfo>();

        /// <summary>
        /// Initializes a new instance of the AsyncRequestQueue class.
        /// </summary>
        /// <param name="requestLimit">Request limit.</param>
        /// <param name="overflowAction">The overflow action.</param>
        public AsyncRequestQueue(int requestLimit, AsyncTargetWrapperOverflowAction overflowAction)
        {
            this.RequestLimit = requestLimit;
            this.OnOverflow = overflowAction;
        }

        /// <summary>
        /// Gets or sets the request limit.
        /// </summary>
        public int RequestLimit { get; set; }

        /// <summary>
        /// Gets or sets the action to be taken when there's no more room in
        /// the queue and another request is enqueued.
        /// </summary>
        public AsyncTargetWrapperOverflowAction OnOverflow { get; set; }

        /// <summary>
        /// Gets the number of requests currently in the queue.
        /// </summary>
        public int RequestCount
        {
            get
            {
                lock (this)
                {
                    return this.logEventInfoQueue.Count;
                }
            }
        }

        /// <summary>
        /// Enqueues another item. If the queue is overflown the appropriate
        /// action is taken as specified by <see cref="OnOverflow"/>.
        /// </summary>
        /// <param name="logEventInfo">The log event info.</param>
        /// <returns>Queue was empty before enqueue</returns>
        public bool Enqueue(AsyncLogEventInfo logEventInfo)
        {
            lock (this)
            {
                if (this.logEventInfoQueue.Count >= this.RequestLimit)
                {
                    InternalLogger.Debug("Async queue is full");
                    switch (this.OnOverflow)
                    {
                        case AsyncTargetWrapperOverflowAction.Discard:
                            InternalLogger.Debug("Discarding one element from queue");
                            this.logEventInfoQueue.Dequeue();
                            break;

                        case AsyncTargetWrapperOverflowAction.Grow:
                            InternalLogger.Debug("The overflow action is Grow, adding element anyway");
                            break;

                        case AsyncTargetWrapperOverflowAction.Block:
                            while (this.logEventInfoQueue.Count >= this.RequestLimit)
                            {
                                InternalLogger.Debug("Blocking because the overflow action is Block...");
                                System.Threading.Monitor.Wait(this);
                                InternalLogger.Trace("Entered critical section.");
                            }

                            InternalLogger.Trace("Limit ok.");
                            break;
                    }
                }

                this.logEventInfoQueue.Enqueue(logEventInfo);
                return this.logEventInfoQueue.Count == 1;
            }
        }

        /// <summary>
        /// Dequeues a maximum of <c>count</c> items from the queue
        /// and adds returns the list containing them.
        /// </summary>
        /// <param name="count">Maximum number of items to be dequeued (-1 means everything).</param>
        /// <returns>The array of log events.</returns>
        public AsyncLogEventInfo[] DequeueBatch(int count)
        {
            AsyncLogEventInfo[] resultEvents;

            lock (this)
            {
                if (count == -1 || this.logEventInfoQueue.Count < count)
                    count = this.logEventInfoQueue.Count;

                if (count == 0)
                    return Internal.ArrayHelper.Empty<AsyncLogEventInfo>();

                resultEvents = new AsyncLogEventInfo[count];
                for (int i = 0; i < count; ++i)
                {
                    resultEvents[i] = this.logEventInfoQueue.Dequeue();
                }

                if (this.OnOverflow == AsyncTargetWrapperOverflowAction.Block)
                {
                    System.Threading.Monitor.PulseAll(this);
                }
            }

            return resultEvents;
        }

        /// <summary>
        /// Dequeues into a preallocated array, instead of allocating a new one
        /// </summary>
        /// <param name="result">Preallocated array</param>
        /// <returns>ArraySegment that specifies how many items that was dequeued.</returns>
        public ArraySegment<AsyncLogEventInfo> DequeueBatch(AsyncLogEventInfo[] result)
        {
            lock (this)
            {
                int count = result.Length;
                if (this.logEventInfoQueue.Count < count)
                    count = this.logEventInfoQueue.Count;
                for (int i = 0; i < count; ++i)
                    result[i] = this.logEventInfoQueue.Dequeue();
                if (this.OnOverflow == AsyncTargetWrapperOverflowAction.Block)
                {
                    System.Threading.Monitor.PulseAll(this);
                }
                return new ArraySegment<AsyncLogEventInfo>(result, 0, count);
            }
        }

        /// <summary>
        /// Clears the queue.
        /// </summary>
        public void Clear()
        {
            lock (this)
            {
                this.logEventInfoQueue.Clear();
            }
        }
    }
}
