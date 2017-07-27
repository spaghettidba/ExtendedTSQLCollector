/*
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. 
 * IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, 
 * OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; 
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, 
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, 
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Andora.UserControlLibrary.Converters
{
    [ValueConversion(typeof(DateTime), typeof(double))]
    public class DateTimeDoubleConverter : IValueConverter
    {
        /// <summary>
        /// Converts a DateTime Value to a Double Value using the Ticks of the DateTime instance.
        /// </summary>
        /// <param name="value">Instance of the DateTime class.</param>
        /// <param name="targetType">Target Type, which should be a Double.</param>
        /// <param name="parameter">Parameter used in the conversion.</param>
        /// <param name="culture">Globalization culture instance.</param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DateTime dt = DateTime.Parse(value.ToString());
            return dt.Ticks;
        }

        /// <summary>
        /// Converts a Double Value to a DateTime Value assuming the Double represents the amount of Ticks for a DateTime instance.
        /// </summary>
        /// <param name="value">Instance of the Double Class.</param>
        /// <param name="targetType">Target Type, which should be a DateTime</param>
        /// <param name="parameter">Parameter used in the conversion.</param>
        /// <param name="culture">Globalization culture instance.</param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double d = double.Parse(value.ToString());
            return new DateTime((long)d);
        }
    }
}
