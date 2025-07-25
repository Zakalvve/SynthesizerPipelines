using Audio.Processing.Components;
using Audio.Processing.Pipelines;

var startNode = new Node<SimpleBuffer<double>, double, double, double>(new AddOne()); //2
var secondNode = new Node<SimpleBuffer<double>, double,double, double>(new MultiplyByTwo()); //4
var thirdNode = new Node<SimpleBuffer<double>, double, double, double>(new MultiplyByTwo()); //8
var fourthNode = new Node<SimpleBuffer<double>, double, double, double>(new MinusOne()); //7
var fifthNode = new Node<SimpleBuffer<double>, double, double, double>(new HalfInput()); //3.5
var sixthNode = new Node<SimpleBuffer<double>, double, double, int>(new ToInteger()); // 3

var multiNode = new MultiNode<double, double>(new Average());
var multiNodeTwo = new MultiNode<double, double>(new Average());

var outputNode = new Node<SimpleBuffer<int>, int, int, double>(new ToDouble());
var outputNodeTwo = new Node<SimpleBuffer<double>, double, double, double>(new PassThrough<double>());
var outputNodeThree = new Node<SimpleBuffer<double>, double, double, double>(new PassThrough<double>());
var outputNodeFour = new Node<SimpleBuffer<double>, double, double, double>(new PassThrough<double>());


startNode.Connect(secondNode);
secondNode.Connect(thirdNode);
thirdNode.Connect(fourthNode);
fourthNode.Connect(fifthNode);
fifthNode.Connect(sixthNode);
sixthNode.Connect(outputNode);
secondNode.Connect(outputNodeTwo);

startNode.Connect(multiNode);
secondNode.Connect(multiNode);
thirdNode.Connect(multiNode);
fourthNode.Connect(multiNode);
fifthNode.Connect(multiNode);

multiNode.Connect(outputNodeThree);

outputNode.Connect(multiNodeTwo);
outputNodeTwo.Connect(multiNodeTwo);
outputNodeThree.Connect(multiNodeTwo);

multiNodeTwo.Connect(outputNodeFour);

// Run the entire pipeline
startNode.Consume(1);

var output = outputNode.GetData();
Console.WriteLine(output);

var outputTwo = outputNodeTwo.GetData();
Console.WriteLine(outputTwo);

var outputThree = outputNodeThree.GetData();
Console.WriteLine(outputThree);

var outputFour = outputNodeFour.GetData();
Console.WriteLine(outputFour);