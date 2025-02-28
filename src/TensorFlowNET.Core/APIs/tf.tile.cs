﻿/*****************************************************************************
   Copyright 2018 The TensorFlow.NET Authors. All Rights Reserved.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
******************************************************************************/
using static Tensorflow.Binding;

namespace Tensorflow
{
    public partial class tensorflow
    {
        public Tensor tile(Tensor input, Tensor multiples, string name = null)
            => gen_array_ops.tile(input, multiples, name);

        public Tensor tile(Tensor input, object[] multiples, string name = null)
            => gen_array_ops.tile(input, ops.convert_to_tensor(multiples), name);

        public Tensor tile(Tensor input, Shape multiples, string name = null) 
        {
            var multiples_tensor = constant_op.constant(multiples);
            return gen_array_ops.tile(input, multiples_tensor, name);
        } 
    }
}
