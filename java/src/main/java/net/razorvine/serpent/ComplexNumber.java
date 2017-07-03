/**
 * Serpent, a Python literal expression serializer/deserializer
 * (a.k.a. Python's ast.literal_eval in Java)
 * Software license: "MIT software license". See http://opensource.org/licenses/MIT
 * @author Irmen de Jong (irmen@razorvine.net)
 */

package net.razorvine.serpent;

import java.io.Serializable;

/**
 * A complex number.
 */
public class ComplexNumber implements Serializable {
	
	private static final long serialVersionUID = 5396759273405612137L;
	
	public double real;
	public double imaginary;
	
	public ComplexNumber(double r, double i) {
		real=r;
		imaginary=i;
	}

	@Override
	public String toString()
	{
		StringBuilder sb=new StringBuilder();
		sb.append(real);
		if(imaginary>0)
			sb.append('+');
		return sb.append(imaginary).append('i').toString();
	}
	
	public double Magnitude() {
		return Math.sqrt(real * real + imaginary * imaginary);
	}

	public void add(ComplexNumber other)
	{
		real += other.real;
		imaginary += other.imaginary;
	}

	public void subtract(ComplexNumber other)
	{
		real -= other.real;
		imaginary -= other.imaginary;
	}

	public void multiply(ComplexNumber other)
	{
		double new_real = real * other.real - imaginary * other.imaginary;
		double new_imaginary = real * other.imaginary + imaginary * other.real;
		real = new_real;
		imaginary = new_imaginary;
	}

	public void divide(ComplexNumber other)
	{
		double new_real = (real * other.real + imaginary * other.imaginary) / (other.real * other.real + other.imaginary * other.imaginary);
		double new_imaginary = (imaginary * other.real - real * other.imaginary) / (other.real * other.real + other.imaginary * other.imaginary);
		real = new_real;
		imaginary = new_imaginary;
	}
	
	@Override
	public boolean equals(Object obj)
	{
		if(!(obj instanceof ComplexNumber))
		{
			return false;
		}
		ComplexNumber other = (ComplexNumber) obj;
		return real==other.real && imaginary==other.imaginary;
	}
	
	@Override
	public int hashCode()
	{
		Double r = Double.valueOf(real);
		Double i = Double.valueOf(imaginary);
		return r.hashCode() ^ i.hashCode();
	}
}
