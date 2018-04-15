// 
// Copyright (c) 2004-2018 Jaroslaw Kowalski <jaak@jkowalski.net>, Kim Christensen, Julian Verdurmen
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

namespace NLog.Layouts
{
    using System;
    using System.Collections.ObjectModel;
    using System.Text;
    using NLog.Common;
    using NLog.Config;
    using NLog.Internal;
    using NLog.LayoutRenderers;

    /// <summary>
    /// Represents a string with embedded placeholders that can render contextual information.
    /// </summary>
    /// <remarks>
    /// This layout is not meant to be used explicitly. Instead you can just use a string containing layout 
    /// renderers everywhere the layout is required.
    /// </remarks>
    [Layout("SimpleLayout")]
    [ThreadAgnostic]
    [ThreadSafe]
    [AppDomainFixedOutput]
    public class SimpleLayout : Layout, IUsesStackTrace
    {
        private string _fixedText;
        private string _layoutText;
        private ConfigurationItemFactory _configurationItemFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleLayout" /> class.
        /// </summary>
        public SimpleLayout()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleLayout" /> class.
        /// </summary>
        /// <param name="txt">The layout string to parse.</param>
        public SimpleLayout(string txt)
            : this(txt, ConfigurationItemFactory.Default)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleLayout"/> class.
        /// </summary>
        /// <param name="txt">The layout string to parse.</param>
        /// <param name="configurationItemFactory">The NLog factories to use when creating references to layout renderers.</param>
        public SimpleLayout(string txt, ConfigurationItemFactory configurationItemFactory)
        {
            _configurationItemFactory = configurationItemFactory;
            Text = txt;
        }

        internal SimpleLayout(LayoutRenderer[] renderers, string text, ConfigurationItemFactory configurationItemFactory)
        {
            _configurationItemFactory = configurationItemFactory;
            SetRenderers(renderers, text);
        }

        /// <summary>
        /// Original text before compile to Layout renderes
        /// </summary>
        public string OriginalText { get; private set; }

        /// <summary>
        /// Gets or sets the layout text.
        /// </summary>
        /// <docgen category='Layout Options' order='10' />
        public string Text
        {
            get => _layoutText;

            set
            {
                OriginalText = value;

                LayoutRenderer[] renderers;
                string txt;
                if (value == null)
                {
                    renderers = ArrayHelper.Empty<LayoutRenderer>();
                    txt = string.Empty;
                }
                else
                {
                    renderers = LayoutParser.CompileLayout(
                       _configurationItemFactory,
                       new SimpleStringReader(value),
                       false,
                       out txt);
                }

                SetRenderers(renderers, txt);
            }
        }
        /// <summary>
        /// Is the message fixed? (no Layout renderers used)
        /// </summary>
        public bool IsFixedText => _fixedText != null;

        /// <summary>
        /// Get the fixed text. Only set when <see cref="IsFixedText"/> is <c>true</c>
        /// </summary>
        public string FixedText => _fixedText;

        /// <summary>
        /// Gets a collection of <see cref="LayoutRenderer"/> objects that make up this layout.
        /// </summary>
        public ReadOnlyCollection<LayoutRenderer> Renderers { get; private set; }

        /// <summary>
        /// Gets the level of stack trace information required for rendering.
        /// </summary>
        public new StackTraceUsage StackTraceUsage => base.StackTraceUsage;

        /// <summary>
        /// Converts a text to a simple layout.
        /// </summary>
        /// <param name="text">Text to be converted.</param>
        /// <returns>A <see cref="SimpleLayout"/> object.</returns>
        public static implicit operator SimpleLayout(string text)
        {
            if (text == null) return null;

            return new SimpleLayout(text);
        }

        /// <summary>
        /// Escapes the passed text so that it can
        /// be used literally in all places where
        /// layout is normally expected without being
        /// treated as layout.
        /// </summary>
        /// <param name="text">The text to be escaped.</param>
        /// <returns>The escaped text.</returns>
        /// <remarks>
        /// Escaping is done by replacing all occurrences of
        /// '${' with '${literal:text=${}'
        /// </remarks>
        public static string Escape(string text)
        {
            return text.Replace("${", "${literal:text=${}");
        }

        /// <summary>
        /// Evaluates the specified text by expanding all layout renderers.
        /// </summary>
        /// <param name="text">The text to be evaluated.</param>
        /// <param name="logEvent">Log event to be used for evaluation.</param>
        /// <returns>The input text with all occurrences of ${} replaced with
        /// values provided by the appropriate layout renderers.</returns>
        public static string Evaluate(string text, LogEventInfo logEvent)
        {
            var layout = new SimpleLayout(text);
            return layout.Render(logEvent);
        }

        /// <summary>
        /// Evaluates the specified text by expanding all layout renderers
        /// in new <see cref="LogEventInfo" /> context.
        /// </summary>
        /// <param name="text">The text to be evaluated.</param>
        /// <returns>The input text with all occurrences of ${} replaced with
        /// values provided by the appropriate layout renderers.</returns>
        public static string Evaluate(string text)
        {
            return Evaluate(text, LogEventInfo.CreateNullEvent());
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"></see> that represents the current object.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"></see> that represents the current object.
        /// </returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Text) && Renderers?.Count > 0)
            {
                return ToStringWithNestedItems(Renderers, r => r.ToString());
            }

            return string.Concat("'", Text, "'");
        }

        internal void SetRenderers(LayoutRenderer[] renderers, string text)
        {
            Renderers = new ReadOnlyCollection<LayoutRenderer>(renderers);

            if (Renderers.Count == 1 && Renderers[0] is LiteralLayoutRenderer)
            {
                _fixedText = ((LiteralLayoutRenderer)Renderers[0]).Text;
            }
            else
            {
                //todo fixedText = null is also used if the text is fixed, but is a empty renderers not fixed?
                _fixedText = null;
            }

            _layoutText = text;

            if (LoggingConfiguration != null)
            {
                PerformObjectScanning();
            }
        }

        /// <summary>
        /// Initializes the layout.
        /// </summary>
        protected override void InitializeLayout()
        {
            for (int i = 0; i < Renderers.Count; i++)
            {
                LayoutRenderer renderer = Renderers[i];
                try
                {
                    renderer.Initialize(LoggingConfiguration);
                }
                catch (Exception exception)
                {
                    //also check IsErrorEnabled, otherwise 'MustBeRethrown' writes it to Error

                    //check for performance
                    if (InternalLogger.IsWarnEnabled || InternalLogger.IsErrorEnabled)
                    {
                        InternalLogger.Warn(exception, "Exception in '{0}.InitializeLayout()'", renderer.GetType().FullName);
                    }

                    if (exception.MustBeRethrown())
                    {
                        throw;
                    }
                }
            }

            base.InitializeLayout();
        }

        internal override void PrecalculateBuilder(LogEventInfo logEvent, StringBuilder target)
        {
            if (!ThreadAgnostic) RenderAppendBuilder(logEvent, target, true);
        }

        /// <summary>
        /// Renders the layout for the specified logging event by invoking layout renderers
        /// that make up the event.
        /// </summary>
        /// <param name="logEvent">The logging event.</param>
        /// <returns>The rendered layout.</returns>
        protected override string GetFormattedMessage(LogEventInfo logEvent)
        {
            if (IsFixedText)
            {
                return _fixedText;
            }

            return RenderAllocateBuilder(logEvent);
        }

        private void RenderAllRenderers(LogEventInfo logEvent, StringBuilder target)
        {
            //Memory profiling pointed out that using a foreach-loop was allocating
            //an Enumerator. Switching to a for-loop avoids the memory allocation.
            for (int i = 0; i < Renderers.Count; i++)
            {
                LayoutRenderer renderer = Renderers[i];
                try
                {
                    renderer.RenderAppendBuilder(logEvent, target);
                }
                catch (Exception exception)
                {
                    //also check IsErrorEnabled, otherwise 'MustBeRethrown' writes it to Error

                    //check for performance
                    if (InternalLogger.IsWarnEnabled || InternalLogger.IsErrorEnabled)
                    {
                        InternalLogger.Warn(exception, "Exception in '{0}.Append()'", renderer.GetType().FullName);
                    }

                    if (exception.MustBeRethrown())
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Renders the layout for the specified logging event by invoking layout renderers
        /// that make up the event.
        /// </summary>
        /// <param name="logEvent">The logging event.</param>
        /// <param name="target"><see cref="StringBuilder"/> for the result</param>
        protected override void RenderFormattedMessage(LogEventInfo logEvent, StringBuilder target)
        {
            if (IsFixedText)
            {
                target.Append(_fixedText);
                return;
            }

            RenderAllRenderers(logEvent, target);
        }
    }
}
