#nullable enable
using System;

namespace Tessa.Extensions.Default.Shared.AbTest
{
    public static class AbRequestTypes
    {
        /// <summary>
        /// Тестовый пример на получение данных от некоторого внешнего сервиса 1С.
        /// </summary>
        public static readonly Guid GetExternalSystemData = new(0x86333B21, 0xA1C5, 0x4698, 0xB0, 0x23, 0xB4, 0x27, 0xC8, 0xBC, 0xCF, 0x94);

        /// <summary>
        /// Создание тестовой информации при нажатии на кнопку тулбара "Получить таблицу" в карточке автомобиля.
        /// </summary>
        public static readonly Guid TestCarTableRequest = // {249CB101-C65E-44E0-8CC4-95BBE17978BF}
            new(0x249cb101, 0xc65e, 0x44e0, 0x8c, 0xc4, 0x95, 0xbb, 0xe1, 0x79, 0x78, 0xbf);

        /// <summary>
        /// Создание тестовых данных из карточки настроек типового решения.
        /// </summary>
        public static readonly Guid TestData = // 207E75B5-ABB8-403A-A12A-897019AFCCF6
            new(0x207e75b5, 0xabb8, 0x403a, 0xa1, 0x2a, 0x89, 0x70, 0x19, 0xaf, 0xcc, 0xf6);
    }
}
