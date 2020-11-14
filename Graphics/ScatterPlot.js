class ScatterPlot{
constructor(data,title,file){
this.data = data;
this.title = title;
this.file = file;
}
  createGraph(){
    //https://bl.ocks.org/sebg/6f7f1dd55e0c52ce5ee0dac2b2769f4b
    var margin = {top: 20, right: 40, bottom: 220, left: 120},
        width = 1000 - margin.left - margin.right,
        height = 550 - margin.top - margin.bottom;

    var x = d3.scaleLinear()
        .range([0, width]);

    var y = d3.scaleLinear()
        .range([height, 0]);

    var xAxis = d3.axisBottom(x).tickFormat(d3.format("d"));

    var yAxis = d3.axisLeft(y).ticks(4).tickFormat(d3.format("d"));
d3.select("body").append("p").text(this.file);
    var svg = d3.select("body").append("svg")
        .attr("width", width + margin.left + margin.right)
        .attr("height", height + margin.top + margin.bottom)
      .append("g")
        .attr("transform", "translate(" + margin.left + "," + margin.top + ")");
        var x = x.domain([0,this.data.length]);
        var y =  y.domain(d3.extent(this.data)).nice();
        svg.append("g")
          .attr("class", "x axis")
          .attr("transform", "translate(0," + height + ")")
          .call(xAxis)
        .append("text")
          .attr("class", "label")
          .attr("x", width)
          .attr("y", 50)
          .style("text-anchor", "end")
          .text("Settlement configuration");

  svg.append("g")
      .attr("class", "y axis")
      .call(yAxis)
    .append("text")
      .attr("class", "label")
      .attr("transform", "rotate(-90)")
      .attr("y", -100)
      .attr("dy", ".71em")
      .style("text-anchor", "end")
      .text("Visibility score");

  svg.selectAll(".dot")
      .data(this.data)
    .enter().append("circle")
      .attr("class", "dot")
      .attr("r", function(d,i) { if (i==0) return 5; else return 1})
      .attr("cx", function(d,i) { return x(i); })
      .attr("cy", function(d) { return y(d); })
      .style("fill", "#808080");

  svg.selectAll(".text")
      .data(this.data)
    .enter().append("text")
      .attr("x", function(d,i) { return x(i)+25; })
      .attr("y", function(d) { return y(d)+25; })
      .attr("dy", ".35em")
      .style("text-anchor", "start")
      .text(function(d,i) {if(i==0) return "actual settlement"; if(i==500) return "random settlement configurations"; });

svg
    .selectAll("medianLines")
    .data(this.data)
    .enter()
    .append("line")
      .attr("x1",function(d,i){if(i==0) return x(i); if(i==500) return x(i); })
      .attr("x2",function(d,i) {if(i==0) return x(i)+25; if(i==500) return x(i)+25; })
      .attr("y1", function(d,i){if(i==0) return y(d); if(i==500) return y(d); })
      .attr("y2", function(d,i){if(i==0) return y(d)+25; if(i==500) return y(d)+25; })
      .attr("stroke", "black")
      .style("stroke-width",  0.5);

      svg.selectAll("text")
.attr("font-size", "15px")

svg.selectAll(".title")
      .data(this.title)
      .enter().append("text")
      .attr("class", "title")
      .attr("x", 0)
      .attr("y", function(d,i) {return height+margin.bottom*0.35+(i*20)})
      .attr("dy", ".35em")
      .style("text-anchor", "start")
      .text(function(d) {return d});

  }  
}