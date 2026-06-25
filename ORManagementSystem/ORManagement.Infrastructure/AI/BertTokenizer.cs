using System.Text.RegularExpressions;

namespace ORManagement.Infrastructure.AI
{
    public class BertTokenizer
    {
        private readonly Dictionary<string, int> _vocab;
        private readonly int _unknownTokenId;
        private readonly int _clsTokenId;
        private readonly int _sepTokenId;
        private readonly int _padTokenId;

        public BertTokenizer(string vocabPath)
        {
            if (!File.Exists(vocabPath))
            {
                throw new FileNotFoundException(
                    "BERT vocab file was not found.",
                    vocabPath);
            }

            _vocab = File.ReadAllLines(vocabPath)
                .Select((token, index) => new
                {
                    Token = token.Trim(),
                    Index = index
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Token))
                .ToDictionary(
                    item => item.Token,
                    item => item.Index,
                    StringComparer.Ordinal);

            _unknownTokenId = GetTokenId("[UNK]");
            _clsTokenId = GetTokenId("[CLS]");
            _sepTokenId = GetTokenId("[SEP]");
            _padTokenId = GetTokenId("[PAD]");
        }

        public BertTokenizedInput Tokenize(
            string text,
            int maxSequenceLength)
        {
            if (maxSequenceLength < 8)
            {
                maxSequenceLength = 8;
            }

            var tokenIds = new List<int>
            {
                _clsTokenId
            };

            foreach (var word in BasicTokenize(text))
            {
                var wordPieceIds = WordPieceTokenize(word);

                foreach (var id in wordPieceIds)
                {
                    tokenIds.Add(id);

                    if (tokenIds.Count >= maxSequenceLength - 1)
                    {
                        break;
                    }
                }

                if (tokenIds.Count >= maxSequenceLength - 1)
                {
                    break;
                }
            }

            tokenIds.Add(_sepTokenId);

            var attentionMask = tokenIds
                .Select(_ => 1)
                .ToList();

            while (tokenIds.Count < maxSequenceLength)
            {
                tokenIds.Add(_padTokenId);
                attentionMask.Add(0);
            }

            var tokenTypeIds = Enumerable
                .Repeat(0, maxSequenceLength)
                .ToList();

            return new BertTokenizedInput
            {
                InputIds = tokenIds.Select(id => (long)id).ToArray(),
                AttentionMask = attentionMask.Select(id => (long)id).ToArray(),
                TokenTypeIds = tokenTypeIds.Select(id => (long)id).ToArray()
            };
        }

        private int GetTokenId(string token)
        {
            return _vocab.TryGetValue(token, out var id)
                ? id
                : 0;
        }

        private static IEnumerable<string> BasicTokenize(string text)
        {
            var normalized = string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.ToLowerInvariant().Trim();

            return Regex
                .Split(normalized, @"(\s+|[,.!?;:()\[\]{}""'])")
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Where(token => !Regex.IsMatch(token, @"^\s+$"));
        }

        private IEnumerable<int> WordPieceTokenize(string word)
        {
            if (_vocab.TryGetValue(word, out var directId))
            {
                return new[] { directId };
            }

            var tokens = new List<int>();
            var start = 0;

            while (start < word.Length)
            {
                var end = word.Length;
                string? currentSubstring = null;

                while (start < end)
                {
                    var substring = word.Substring(start, end - start);

                    if (start > 0)
                    {
                        substring = "##" + substring;
                    }

                    if (_vocab.ContainsKey(substring))
                    {
                        currentSubstring = substring;
                        break;
                    }

                    end--;
                }

                if (currentSubstring is null)
                {
                    return new[] { _unknownTokenId };
                }

                tokens.Add(_vocab[currentSubstring]);
                start = end;
            }

            return tokens;
        }
    }

    public class BertTokenizedInput
    {
        public long[] InputIds { get; set; } = Array.Empty<long>();

        public long[] AttentionMask { get; set; } = Array.Empty<long>();

        public long[] TokenTypeIds { get; set; } = Array.Empty<long>();
    }
}