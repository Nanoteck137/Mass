#include <stdio.h>
#include <stdint.h>

void test(float x)
{
	x += 2.2;
}

int main(int argc, char **argv)
{
	float x = 3.14;
	int y = 3;
	float z = x + y;

	printf("%f\n", z);
	test(z);

	return 0;
}