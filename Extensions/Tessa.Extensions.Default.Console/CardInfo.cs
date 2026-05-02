using System;

namespace Tessa.Extensions.Default.Console
{
    public sealed class CardInfo
    {
        #region Constructors

        public CardInfo(Guid cardID, string? cardName = null)
        {
            this.CardID = cardID;
            var cardNameFixed = cardName?.Trim();

            if (string.IsNullOrEmpty(cardNameFixed))
            {
                cardNameFixed = cardID.ToString();
            }

            this.CardName = cardNameFixed;
        }

        #endregion

        #region Properties

        public Guid CardID { get; }
        
        public string CardName { get; set; }

        #endregion
    }
}