#nullable enable

using System;

namespace Tessa.Extensions.Default.Server.Workflow.KrTaskManager.Managers
{
    /// <summary>
    /// Варианты завершения действий на основе <see cref="IKrTaskManager{T}"/>.
    /// </summary>
    public static class KrTaskManagerCompletionOptions
    {
        /// <summary>
        /// Идентификатор варианта завершения действия при запросе комментария.
        /// </summary>
        public static readonly Guid RequestComment = new(0xcd3cb65b, 0xc776, 0x4772, 0xbd, 0x2f, 0xbc, 0x39, 0xef, 0xbe, 0x65, 0xb6);

        /// <summary>
        /// Идентификатор варианта завершения действия при обработки отмены.
        /// </summary>
        public static readonly Guid Cancel = new(0xab0739e7, 0xc1b6, 0x44b6, 0x80, 0xa1, 0x2e, 0x9e, 0x5e, 0xf7, 0x68, 0x88);

        /// <summary>
        /// Идентификатор варианта завершения действия при отправке дочернего задания дополнительного согласования.
        /// </summary>
        public static readonly Guid RequestAdditionalApproval = new(0xa09cac62, 0x8a3f, 0x4ebb, 0xab, 0x91, 0x30, 0xdc, 0x47, 0xce, 0xe3, 0x66);

        /// <summary>
        /// Идентификатор варианта завершения действия при отрицательном завершении обработки.
        /// </summary>
        public static readonly Guid NegativeResult = new(0xcd99d706, 0xc3d3, 0x481d, 0xab, 0x3, 0xa8, 0x70, 0xd6, 0xa8, 0xc5, 0xa9);

        /// <summary>
        /// Идентификатор варианта завершения действия при положительном завершении обработки.
        /// </summary>
        public static readonly Guid PositiveResult = new(0xdf565fcb, 0x5924, 0x42a1, 0x9f, 0xe0, 0xc, 0xc7, 0xe7, 0x7b, 0x79, 0x22);

        /// <summary>
        /// Идентификатор варианта завершения действия при отрицательном завершении обработки отдельного задания.
        /// </summary>
        public static readonly Guid IntermediateNegativeResult = new(0x2d03f938, 0x1e49, 0x47d4, 0xa8, 0x6, 0xa5, 0xc1, 0x5b, 0x56, 0x21, 0x28);

        /// <summary>
        /// Идентификатор варианта завершения действия при положительном завершении обработки отдельного задания.
        /// </summary>
        public static readonly Guid IntermediatePositiveResult = new(0xe9ebaf4d, 0x95f9, 0x46fa, 0xb2, 0x74, 0x35, 0x5e, 0x7a, 0xb0, 0x28, 0x6d);

        /// <summary>
        /// Идентификатор варианта завершения действия при отправке на доработку после положительного завершении обработки.
        /// </summary>
        public static readonly Guid EditAfterPositiveResult = new(0x2966ae2a, 0x189e, 0x4c64, 0xb6, 0x81, 0xf3, 0x75, 0x13, 0xca, 0x5e, 0xbb);

        /// <summary>
        /// Идентификатор варианта завершения действия при отправке на доработку при отрицательном завершении обработки.
        /// </summary>
        public static readonly Guid ReturnAfterNegativeResult = new(// {AEB79AAD-0CFB-4682-8305-8682721984D6}
            0xaeb79aad, 0xcfb, 0x4682, 0x83, 0x5, 0x86, 0x82, 0x72, 0x19, 0x84, 0xd6);

        /// <summary>
        /// Идентификатор варианта завершения действия при делегировании задания.
        /// </summary>
        public static readonly Guid Delegate = new(0xeb1a185d, 0x9a4e, 0x4278, 0x99, 0x51, 0x1d, 0x64, 0xc6, 0x7c, 0x31, 0x58);

        /// <summary>
        /// Идентификатор варианта завершения действия при отправке задания следующему исполнителю.
        /// </summary>
        public static readonly Guid SendNextTask = new(0xf65ede44, 0x7d69, 0x4111, 0xa4, 0x8e, 0x91, 0xe9, 0x5b, 0x5f, 0x39, 0xc9);

        /// <summary>
        /// Идентификатор варианта завершения действия при завершении обработки.
        /// </summary>
        public static readonly Guid Complete = new(0x528a9ffa, 0x64a0, 0x4507, 0xa0, 0xe1, 0x37, 0xb6, 0xfd, 0x2d, 0xa1, 0x73);

        /// <summary>
        /// Идентификатор варианта завершения действия при добавлении комментария.
        /// </summary>
        public static readonly Guid AddComment = new(0xbcd695c5, 0xa901, 0x404f, 0xac, 0xa9, 0xcf, 0xfa, 0xaa, 0xd5, 0xae, 0x98);

        /// <summary>
        /// Идентификатор варианта завершения действия при отзыве задания согласования.
        /// </summary>
        public static readonly Guid RevokeResult = // {509F9392-5FB6-4F08-B630-A32FE46AFB33}
            new(0x509f9392, 0x5fb6, 0x4f08, 0xb6, 0x30, 0xa3, 0x2f, 0xe4, 0x6a, 0xfb, 0x33);

        /// <summary>
        /// Идентификатор варианта завершения действия при удалении задания.
        /// </summary>
        public static readonly Guid Delete = // {8BB780D3-2847-4FC2-9E2E-FE9E2A66FC2A}
            new(0x8bb780d3, 0x2847, 0x4fc2, 0x9e, 0x2e, 0xfe, 0x9e, 0x2a, 0x66, 0xfc, 0x2a);
    }
}
