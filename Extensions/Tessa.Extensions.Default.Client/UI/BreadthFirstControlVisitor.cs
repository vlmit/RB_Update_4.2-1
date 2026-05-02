#nullable enable
using System.Collections.Generic;
using Tessa.UI.Cards;
using Tessa.UI.Cards.Controls;

namespace Tessa.Extensions.Default.Client.UI
{
    public abstract class BreadthFirstControlVisitor
    {
        #region Private Methods

        private void VisitInternal(Queue<object> queue)
        {
            while (queue.Count != 0)
            {
                var item = queue.Dequeue();
                switch (item)
                {
                    case IControlViewModel controlViewModel:
                        this.VisitControl(controlViewModel);

                        switch (controlViewModel)
                        {
                            case TabControlViewModel tabViewModel:
                                EnqueueTabs(tabViewModel, queue);
                                break;

                            case ContainerViewModel containerControl:
                                EnqueueForm(containerControl.Form, queue);
                                break;
                        }

                        break;

                    case IBlockViewModel blockViewModel:
                        this.VisitBlock(blockViewModel);
                        EnqueueBlock(blockViewModel, queue);
                        break;
                }
            }
        }

        private static void EnqueueTabs(TabControlViewModel tabControl, Queue<object> queue)
        {
            foreach (var tabControlTab in tabControl.Tabs)
            {
                EnqueueForm(tabControlTab, queue);
            }
        }

        private static void EnqueueForm(
            IFormWithBlocksViewModel formViewModel,
            Queue<object> queue)
        {
            foreach (var blockViewModel in formViewModel.Blocks)
            {
                queue.Enqueue(blockViewModel);
            }
        }

        private static void EnqueueBlock(
            IBlockViewModel blockViewModel,
            Queue<object> queue)
        {
            foreach (var controlViewModel in blockViewModel.Controls)
            {
                queue.Enqueue(controlViewModel);
            }
        }

        #endregion

        #region Protected Declarations

        protected abstract void VisitControl(IControlViewModel controlViewModel);

        protected abstract void VisitBlock(IBlockViewModel blockViewModel);

        #endregion

        #region Methods

        public void Visit(ICardModel cardModel)
        {
            var queue = new Queue<object>();
            foreach (var block in cardModel.Blocks)
            {
                EnqueueBlock(block.Value, queue);
            }

            foreach (var form in cardModel.Forms)
            {
                EnqueueForm(form, queue);
            }

            this.VisitInternal(queue);
        }

        public void Visit(IControlViewModel rootControl)
        {
            var queue = new Queue<object>();
            switch (rootControl)
            {
                case TabControlViewModel tabControl:
                    EnqueueTabs(tabControl, queue);
                    break;

                case ContainerViewModel containerControl:
                    EnqueueForm(containerControl.Form, queue);
                    break;
            }

            this.VisitInternal(queue);
        }

        public void Visit(IFormWithBlocksViewModel rootForm)
        {
            var queue = new Queue<object>();
            EnqueueForm(rootForm, queue);
            this.VisitInternal(queue);
        }

        public void Visit(IBlockViewModel rootBlock)
        {
            var queue = new Queue<object>();
            EnqueueBlock(rootBlock, queue);

            this.VisitInternal(queue);
        }

        #endregion
    }
}
