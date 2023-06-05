using System;
using System.Collections.Generic;
using System.Linq;
using Tensorflow;
using Tensorflow.Keras.ArgsDefinition;
using Tensorflow.Keras.Callbacks;
using Tensorflow.Keras.Engine.DataAdapters;
using Tensorflow.Keras.Layers;
using Tensorflow.Keras.Utils;
using Tensorflow.NumPy;
using static Tensorflow.Binding;

namespace Tensorflow.Keras.Engine
{
    public partial class Model
    {
        /// <summary>
        /// Returns the loss value & metrics values for the model in test mode.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="batch_size"></param>
        /// <param name="verbose"></param>
        /// <param name="steps"></param>
        /// <param name="max_queue_size"></param>
        /// <param name="workers"></param>
        /// <param name="use_multiprocessing"></param>
        /// <param name="return_dict"></param>
        /// <param name="is_val"></param>
        public Dictionary<string, float> evaluate(Tensor x, Tensor y,
            int batch_size = -1,
            int verbose = 1,
            int steps = -1,
            int max_queue_size = 10,
            int workers = 1,
            bool use_multiprocessing = false,
            bool return_dict = false,
            bool is_val = false
            )
        {
            if (x.dims[0] != y.dims[0])
            {
                throw new InvalidArgumentError(
                    $"The array x and y should have same value at dim 0, but got {x.dims[0]} and {y.dims[0]}");
            }
            var data_handler = new DataHandler(new DataHandlerArgs
            {
                X = x,
                Y = y,
                BatchSize = batch_size,
                StepsPerEpoch = steps,
                InitialEpoch = 0,
                Epochs = 1,
                MaxQueueSize = max_queue_size,
                Workers = workers,
                UseMultiprocessing = use_multiprocessing,
                Model = this,
                StepsPerExecution = _steps_per_execution
            });

            var callbacks = new CallbackList(new CallbackParams
            {
                Model = this,
                Verbose = verbose,
                Steps = data_handler.Inferredsteps
            });

            return evaluate(data_handler, callbacks, is_val, test_function);
        }

        public Dictionary<string, float> evaluate(IEnumerable<Tensor> x, Tensor y, int verbose = 1, bool is_val = false)
        {
            var data_handler = new DataHandler(new DataHandlerArgs
            {
                X = new Tensors(x),
                Y = y,
                Model = this,
                StepsPerExecution = _steps_per_execution
            });

            var callbacks = new CallbackList(new CallbackParams
            {
                Model = this,
                Verbose = verbose,
                Steps = data_handler.Inferredsteps
            });

            return evaluate(data_handler, callbacks, is_val, test_step_multi_inputs_function);
        }

        public Dictionary<string, float> evaluate(IDatasetV2 x, int verbose = 1, bool is_val = false)
        {
            var data_handler = new DataHandler(new DataHandlerArgs
            {
                Dataset = x,
                Model = this,
                StepsPerExecution = _steps_per_execution
            });

            var callbacks = new CallbackList(new CallbackParams
            {
                Model = this,
                Verbose = verbose,
                Steps = data_handler.Inferredsteps
            });

            return evaluate(data_handler, callbacks, is_val, test_function);
        }

        /// <summary>
        /// Internal bare implementation of evaluate function.
        /// </summary>
        /// <param name="data_handler">Interations handling objects</param>
        /// <param name="callbacks"></param>
        /// <param name="test_func">The function to be called on each batch of data.</param>
        /// <param name="is_val">Whether it is validation or test.</param>
        /// <returns></returns>
        Dictionary<string, float> evaluate(DataHandler data_handler, CallbackList callbacks, bool is_val, Func<DataHandler, Tensor[], Dictionary<string, float>> test_func)
        {
            callbacks.on_test_begin();

            var results = new Dictionary<string, float>();
            var logs = results;
            foreach (var (epoch, iterator) in data_handler.enumerate_epochs())
            {
                reset_metrics();
                callbacks.on_epoch_begin(epoch);
                // data_handler.catch_stop_iteration();

                foreach (var step in data_handler.steps())
                {
                    callbacks.on_test_batch_begin(step);

                    var data = iterator.next();

                    logs = test_func(data_handler, iterator.next());

                    tf_with(ops.control_dependencies(Array.Empty<object>()), ctl => _train_counter.assign_add(1));

                    var end_step = step + data_handler.StepIncrement;
                    if (!is_val)
                        callbacks.on_test_batch_end(end_step, logs);
                }

                if (!is_val)
                    callbacks.on_epoch_end(epoch, logs);
            }

            foreach (var log in logs)
            {
                results[log.Key] = log.Value;
            }

            return results;
        }

        Dictionary<string, float> test_function(DataHandler data_handler, Tensor[] data)
        {
            var (x, y) = data_handler.DataAdapter.Expand1d(data[0], data[1]);

            var y_pred = Apply(x, training: false);
            var loss = compiled_loss.Call(y, y_pred);

            compiled_metrics.update_state(y, y_pred);

            var outputs = metrics.Select(x => (x.Name, x.result())).ToDictionary(x => x.Name, x => (float)x.Item2);
            return outputs;
        }

        Dictionary<string, float> test_step_multi_inputs_function(DataHandler data_handler, Tensor[] data)
        {
            var x_size = data_handler.DataAdapter.GetDataset().FirstInputTensorCount;
            var outputs = train_step(data_handler, new Tensors(data.Take(x_size)), new Tensors(data.Skip(x_size)));
            return outputs;
        }
    }
}
