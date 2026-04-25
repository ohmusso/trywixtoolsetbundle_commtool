using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace WpfApp1.Models
{
    public class ConfigRoot
    {
        [Required]
        [StringLength(20, MinimumLength = 1)]
        public string name { get; set; }

        [Required]
        public bool? enableDebug { get; set; }

        [Required]
        [MaxLength(10, ErrorMessage = "datasは10個以下である必要があります。")]
        public List<DataItem> datas { get; set; }
    }
    public class DataItem : IValidatableObject
    {
        [Required]
        [StringLength(20, MinimumLength = 1)]
        public string name { get; set; }

        [Required]
        [RegularExpression("char|short|int", ErrorMessage = "typeは'char', 'short', 'int'のいずれかである必要があります。")]
        public string type { get; set; }

        [Required]
        public string value { get; set; } // JSON例に合わせてstringで定義

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool isParsed = false;
            long numericValue = 0;

            // 数値として妥当かチェック
            if (long.TryParse(value, out numericValue))
            {
                isParsed = true;
            }

            // 型ごとの許容範囲チェック
            var rangeError = new ValidationResult($"{type} の許容範囲外、または数値形式が正しくありません: {value}", new[] { nameof(value) });

            switch (type)
            {
                case "char": // 1バイト符号付 (-128 to 127)
                    if (!isParsed || numericValue < sbyte.MinValue || numericValue > sbyte.MaxValue)
                        yield return rangeError;
                    break;

                case "short": // 2バイト符号付 (-32,768 to 32,767)
                    if (!isParsed || numericValue < short.MinValue || numericValue > short.MaxValue)
                        yield return rangeError;
                    break;

                case "int": // 4バイト符号付 (-2,147,483,648 to 2,147,483,647)
                    if (!isParsed || numericValue < int.MinValue || numericValue > int.MaxValue)
                        yield return rangeError;
                    break;
            }
        }
    }
}
