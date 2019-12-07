#include <stdio.h>

struct Test {
	int a;
	int b;

	void print() {
		printf("A: %d B: %d\n", a, b);
	}
};

int main(int argc, char **argv)
{
	Test test;
	test.a = 123;
	test.b = 312;
	test.print();
    return 0;
}
