// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MatchedNode.cs" company="Innovative Data Solutions">
//   Copyright © 2013 Innovative Data Solutions, Inc.
// </copyright>
// <summary>
//   A Matched Node
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Ids.Localization.Compiler
{
    public class MatchedNode
    {
        private readonly string _Match;

        public string Match
        {
            get
            {
                return this._Match;
            }
        }

        public string ReplacementText
        {
            get
            {
                return this._ReplacementText;
            }
        }

        private readonly string _ReplacementText;

        public MatchedNode(string match, string replacementText)
        {
            this._Match = match;
            _ReplacementText = replacementText;
        }
    }
}