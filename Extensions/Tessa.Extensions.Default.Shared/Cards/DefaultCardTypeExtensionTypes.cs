using System;
using Tessa.Cards;

namespace Tessa.Extensions.Default.Shared.Cards
{
    public static class DefaultCardTypeExtensionTypes
    {
        #region Static Fields

        /// <summary>
        /// Расширение, которое наделяет элемент управления "Представление" функциями, аналогичными элементу управления "Список файлов"
        /// </summary>
        public static readonly CardTypeExtensionType InitializeFilesView =
            new(new Guid(0x5e2f5766, 0xb107, 0x4dd1, 0xa7, 0x41, 0x65, 0x5e, 0x83, 0x91, 0xfa, 0xe5),
                nameof(InitializeFilesView),
                [CardInstanceType.Card, CardInstanceType.Dialog, CardInstanceType.Task]);

        /// <summary>
        /// Расширение, которое добавляет представлению функциональность истории заданий.
        /// </summary>
        public static readonly CardTypeExtensionType MakeViewTaskHistory =
            new(new Guid(0x621e7a1b, 0x7980, 0x441c, 0x9a, 0x57, 0xb6, 0x7e, 0x84, 0x7e, 0xe7, 0x73),
                nameof(MakeViewTaskHistory),
                [CardInstanceType.Card]);

        /// <summary>
        /// Расширение, позволяющее открывать карточки из представления.
        /// </summary>
        public static readonly CardTypeExtensionType OpenCardInView =
            new(new Guid(0x9df93a75, 0xf788, 0x4b06, 0xbd, 0x17, 0x88, 0x1a, 0x4, 0x58, 0xd0, 0x46),
                nameof(OpenCardInView),
                [CardInstanceType.Card, CardInstanceType.Task]);

        #endregion

        #region RegisterInternal Method

        /// <summary>
        /// Регистрирует все стандартные типы посредством заданного метода.
        /// </summary>
        /// <param name="registerAction">Метод, выполняющий регистрацию типа.</param>
        public static void Register(Action<CardTypeExtensionType> registerAction)
        {
            foreach (CardTypeExtensionType extensionType
                     in new[]
                     {
                         InitializeFilesView,
                         MakeViewTaskHistory,
                         OpenCardInView
                     })
            {
                registerAction(extensionType);
            }
        }

        #endregion
    }
}
